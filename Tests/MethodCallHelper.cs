using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NLog;

namespace Tests {
    internal sealed class MethodCallHelper {
        private readonly Assembly _assembly;
        private readonly Dictionary<MethodInfo, Byte[]> _methodByteCode;
        private readonly List<Tuple<MethodInfo, MethodInfo>> _calls;
        private readonly ILookup<MethodInfo, MethodInfo> _callsLookup;
        private readonly Logger _logger;

        private static readonly Dictionary<Int16, OpCode> _opcodes;

        static MethodCallHelper() {
            _opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(fieldInfo => fieldInfo.GetValue(null))
                .Cast<OpCode>()
                .ToDictionary(opcode => opcode.Value);
        }

        internal MethodCallHelper(Assembly assembly) {
            _logger = LogManager.GetLogger(GetType().Name);

            _assembly = assembly;
            _methodByteCode = assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Instance
                                              | BindingFlags.NonPublic
                                              | BindingFlags.Public
                                              | BindingFlags.Static
                                              | BindingFlags.DeclaredOnly))
                .Where(mi => mi.GetMethodBody() != null)
                .ToDictionary(mi => mi,
                              mi => mi.GetMethodBody().GetILAsByteArray());

            _calls = new List<Tuple<MethodInfo, MethodInfo>>(_methodByteCode.Count);
            FindCalls();

            _callsLookup = _calls.ToLookup(x => x.Item1, x => x.Item2);
        }

        internal IEnumerable<MemberInfo> GetRecursiveCalls() {
            return _calls.AsParallel()
                .Where(x => x.Item1 == x.Item2)
                .Distinct()
                .Select(x => x.Item1);
        }

        internal IEnumerable<MethodInfo> GetIndirectRecursiveCalls() {
            return _calls.AsParallel()
                .Where(pair => _callsLookup[pair.Item2].Any(x => x != pair.Item2 && x == pair.Item1))
                .Distinct()
                .Select(x => x.Item1);
        }

        private void FindCalls() {
            foreach (var kvp in _methodByteCode) {
                var offset = 0;
                var byteCodes = kvp.Value;

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
                                    var operand       = BitConverter.ToInt32(byteCodes, offset + instruction.Size);
                                    var callingMethod = kvp.Key.GetBaseDefinition();

                                    var resolved = ResolveMethod(callingMethod, operand);
                                    if (resolved != null) {
                                        _calls.Add(Tuple.Create(kvp.Key, resolved));
                                    }
                                }

                                break;
                        }

                        offset += _opcodes[opcode].Size + operandSize;
                    } else {
                        _logger.Warn("Found an unknown opcode: 0x{0:x}", opcode);
                        throw new NotSupportedException(String.Format("Found an unknown opcode: 0x{0:x}", opcode));
                    }
                }
            }
        }

        private MethodInfo ResolveMethod(MethodInfo callingMethod, Int32 operand) {
            MethodBase called = null;

            foreach (var module in _assembly.GetModules()) {
                try {
                    called = module.ResolveMethod(operand,
                                                   callingMethod.DeclaringType.GetGenericArguments(),
                                                   callingMethod.GetGenericArguments());
                } catch (ArgumentException) {
                    // Out of scope
                } catch (MissingMemberException ex) {
                    _logger.Warn("A member seem to have been lost; out-of-date assembly? {0}", ex.Message);
                }

                if (called != null) break;
            }

            return called as MethodInfo;
        }
    }
}
