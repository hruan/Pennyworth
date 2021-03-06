﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NLog;

namespace Pennyworth.Inspection {
	/// <summary>
	/// From the types in an assembly, get the methods and create caller-callee method pairs
	/// </summary>
	/// <remarks>
	/// To find out which method is being called, we go from the calling method's IL code,
	/// find the call instructions and resolve the metadata token.  As we're only interested
	/// in finding out recursive and indirect recursive calls, metadata tokens are resolved
	/// using the referenced assembly's modules.
	/// </remarks>
	internal sealed class InspectionHelper {
		private readonly Assembly _assembly;
		private readonly IDictionary<MethodInfo, Byte[]> _methodILBytes;
		private readonly ICollection<Tuple<MethodInfo, MethodInfo>> _calls;
		private readonly ILookup<MethodInfo, MethodInfo> _callsLookup;
		private readonly Logger _logger;

		private static readonly IDictionary<Int16, OpCode> _opcodes;

		/// <summary>
		/// Map bytecode to OpCode structure when the type is loaded
		/// </summary>
		static InspectionHelper()
		{
			_opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
				.Select(fieldInfo => fieldInfo.GetValue(null))
				.Cast<OpCode>()
				.ToDictionary(opcode => opcode.Value);
		}

		/// <summary>
		/// Instantiate dependencies, resolve method metadata tokens and from that build
		/// caller-callee method pairs
		/// </summary>
		/// <param name="assembly">the assembly that is being probed</param>
		internal InspectionHelper(Assembly assembly)
		{
			_logger = LogManager.GetLogger(GetType().Name);

			_assembly = assembly;
			_methodILBytes = assembly.GetTypes()
				.SelectMany(t => t.GetMethods(BindingFlags.Instance
											  | BindingFlags.NonPublic
											  | BindingFlags.Public
											  | BindingFlags.Static
											  | BindingFlags.DeclaredOnly))
				.Where(mi => mi.GetMethodBody() != null)
				.ToDictionary(mi => mi,
							  mi => mi.GetMethodBody().GetILAsByteArray());

			_calls = new List<Tuple<MethodInfo, MethodInfo>>(_methodILBytes.Count);
			FindCalls();

			_callsLookup = _calls.ToLookup(x => x.Item1, x => x.Item2);
		}

		/// <summary>
		/// Find the recursive methods in the method pairs
		/// </summary>
		/// <returns>recursive methods as <see cref="IEnumerable{T}">IEnumerable</see> of MemberInfo</returns>
		internal IEnumerable<MethodInfo> GetRecursiveCalls()
		{
			return _calls.Where(x => x.Item1 == x.Item2)
				.Distinct()
				.Select(x => x.Item1);
		}

		/// <summary>
		/// Find indirect recursive method call
		/// </summary>
		/// <returns>indirect methods as <see cref="IEnumerable{T}">IEnumerable</see> of MethodInfo</returns>
		internal IEnumerable<MethodInfo> GetIndirectRecursiveCalls()
		{
			return _calls.Where(pair => _callsLookup[pair.Item2].Any(x => x != pair.Item2 && x == pair.Item1))
				.Distinct()
				.Select(x => x.Item1);
		}

		internal IEnumerable<FieldInfo> GetPublicFields()
		{
			return _assembly.GetTypes()
				.Where(t => !t.IsNested)
				.SelectMany(t => t.GetFields(BindingFlags.Instance
											 | BindingFlags.Public
											 | BindingFlags.DeclaredOnly))
				// Apparently, enums have a special public field named value__
				.Where(fi => fi.DeclaringType != null
							 && !fi.DeclaringType.IsEnum);
		}

		/// <summary>
		/// Builds the caller-caller pairs
		/// </summary>
		/// <exception cref="NotSupportedException">Encountered unknown bytecode</exception>
		private void FindCalls()
		{
			foreach (var kvp in _methodILBytes) {
				GetCalledMethods(kvp.Key, kvp.Value)
					.Where(x => x != null)
					.ToList()
					.ForEach(mi => _calls.Add(Tuple.Create(kvp.Key, mi)));
			}
		}

		/// <summary>
		/// Find call instructions within a method's byte codes and attempt to resolve the called
		/// method.
		/// </summary>
		/// <param name="caller"><see cref="MethodInfo"/> of calling method</param>
		/// <param name="byteCodes">IL of calling method as a byte array</param>
		/// <returns></returns>
		private IEnumerable<MethodInfo> GetCalledMethods(MethodInfo caller, Byte[] byteCodes)
		{
			var offset = 0;
			while (offset < byteCodes.Length) {
				Int16 opcode = byteCodes[offset];

				// Multibyte opcode?
				// http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf
				// Partition III, Table 1
				if (opcode == OpCodes.Prefix1.Value) {
					opcode = (Int16) (opcode << 8 | byteCodes[offset + 1]);
				}

				var operandSize = 4;
				if (_opcodes.ContainsKey(opcode)) {
					var instruction = _opcodes[opcode];

					// We're only interested in call instructions but we'll have to
					// adjust offset accordingly
					switch (instruction.OperandType) {
						case OperandType.InlineNone:
							operandSize = 0;
							break;

						case OperandType.ShortInlineBrTarget:
						case OperandType.ShortInlineI:
						case OperandType.ShortInlineVar:
							operandSize = 1;
							break;

						case OperandType.InlineVar:
							operandSize = 2;
							break;

						case OperandType.InlineI8:
						case OperandType.InlineR:
							operandSize = 8;
							break;

						case OperandType.InlineSwitch:
							operandSize += BitConverter.ToInt32(byteCodes, offset + instruction.Size) * 4;
							break;

						case OperandType.InlineMethod:
							if (instruction.FlowControl == FlowControl.Call) {
								var operand = BitConverter.ToInt32(byteCodes, offset + instruction.Size);
								var callingMethod = caller.GetBaseDefinition();

								yield return ResolveMethod(callingMethod, operand);
							}

							break;
					}

					offset += _opcodes[opcode].Size + operandSize;
				} else {
					_logger.Warn("Found an unknown opcode 0x{0:x} in {1}::{2} of {3}.",
								 opcode,
								 caller.DeclaringType.FullName,
								 caller.Name,
								 _assembly.FullName);
					throw new NotSupportedException(String.Format("Found an unknown opcode: 0x{0:x}", opcode));
				}
			}
		}

		/// <summary>
		/// Resolve metadata token using the assembly's modules
		/// </summary>
		/// <param name="callingMethod">the caller</param>
		/// <param name="metadataToken">metadata token for the method being called</param>
		/// <returns></returns>
		private MethodInfo ResolveMethod(MethodInfo callingMethod, Int32 metadataToken)
		{
			MethodBase called = null;

			foreach (var module in _assembly.GetModules()) {
				try {
					called = module.ResolveMethod(metadataToken,
												   callingMethod.DeclaringType.GetGenericArguments(),
												   callingMethod.GetGenericArguments());
				} catch (ArgumentException) {
					// Out of scope
				} catch (MissingMemberException ex) {
					_logger.Warn("A member seem to have been lost; out-of-date assembly? {0}", ex.Message);
				}

				if (called != null)
					break;
			}

			return called as MethodInfo;
		}
	}
}
