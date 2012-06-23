using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Pennyworth {
    public sealed class AssemblyTest : MarshalByRefObject {
        private readonly Assembly _assembly;
        private readonly IEnumerable<FieldInfo> _publicFields;
        private static readonly Dictionary<Int16, OpCode> _opcodes;

        static AssemblyTest() {
            _opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(fieldInfo => fieldInfo.GetValue(null))
                .Cast<OpCode>()
                .ToDictionary(opcode => opcode.Value);
        }

        public AssemblyTest(String path) {
            Debug.Print("Loading {0}", path);

            try {
                _assembly = Assembly.LoadFrom(path);
                _publicFields = _assembly.GetTypes()
                    .SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.Public))
                    // Apparently, enums have a special public field named value__
                    .Where(fi => fi.DeclaringType != null && !fi.DeclaringType.IsEnum);
            } catch (ArgumentException argumentException) {
                Debug.WriteLine(argumentException.ToString());
            } catch (IOException ioException) {
                Debug.WriteLine(ioException.ToString());
            } catch (BadImageFormatException badImage) {
                Debug.WriteLine(badImage.ToString());
            }
        }

        public IEnumerable<OffendingMember> GetPublicFields() {
            return _publicFields.Select(fi => new OffendingMember {
                Path = _assembly.Location,
                Name = fi.Name,
                DeclaringType = fi.DeclaringType.ToString()
            }).ToList();
        }

        public Boolean HasPublicFields() {
            return _publicFields != null && _publicFields.Any();
        }

        public IEnumerable<OffendingMember> GetRecursiveMembers() {
            Debug.Print("Executing in AppDomain {0}", AppDomain.CurrentDomain.FriendlyName);
            var methodByteCodeMap = _assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Instance
                                              | BindingFlags.NonPublic
                                              | BindingFlags.Public
                                              | BindingFlags.Static
                                              | BindingFlags.DeclaredOnly))
                .Where(mi => mi.GetMethodBody() != null)
                .ToDictionary(mi => mi,
                              mi => mi.GetMethodBody().GetILAsByteArray());
            var recursiveMethods = new List<MethodInfo>();

            foreach (var kvp in methodByteCodeMap) {
                var pos = 0;
                var byteCodes = kvp.Value;

                while (pos < byteCodes.Length) {
                    Int16 opcode = byteCodes[pos];

                    // Multibyte opcode?
                    // http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf
                    // Partition III, Table 1
                    if ((opcode & 0xfe) == 0) {
                        opcode = (Int16) ((opcode << 8) | byteCodes[pos + 1]);
                    }

                    var operandSize = 4;
                    if (_opcodes.ContainsKey(opcode)) {
                        var instruction = _opcodes[opcode];

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
                                operandSize = BitConverter.ToInt32(byteCodes, pos + instruction.Size) * 4;
                                break;

                            case OperandType.InlineMethod:
                                if (instruction.FlowControl == FlowControl.Call) {
                                    var operand       = BitConverter.ToInt32(byteCodes, pos + instruction.Size);
                                    var callingMethod = kvp.Key.GetBaseDefinition();

                                    if (IsRecursiveCall(callingMethod, operand)) recursiveMethods.Add(kvp.Key);// yield return kvp.Key;
                                }

                                break;
                        }

                        pos += _opcodes[opcode].Size + operandSize;
                    } else {
                        pos += (opcode & 0xff00) == 0 ? 1 : 2;
                    }
                }
            }

            return recursiveMethods.Select(mi => new OffendingMember {
                Name = mi.Name,
                DeclaringType = mi.DeclaringType.ToString(),
                Path = mi.ReflectedType.Assembly.Location
            }).ToList();
        }

        private static Boolean IsRecursiveCall(MethodInfo callingMethod, Int32 operand) {
            if (callingMethod.DeclaringType != null) {
                MethodBase calledMethod = null;
                try {
                    calledMethod = callingMethod.Module.ResolveMethod(operand,
                                                                      callingMethod.DeclaringType.GetGenericArguments(),
                                                                      callingMethod.GetGenericArguments());
                } catch (ArgumentException ex) {
                    // Out of scope
                    Debug.Print("Couldn't resolve method call in {0}: {1}", callingMethod.Name, ex.Message);
                }

                return calledMethod != null
                       && callingMethod == calledMethod;
                /* if (calledMethod != null
                    && calledMethod.ReflectedType.AssemblyQualifiedName == callingMethod.ReflectedType.AssemblyQualifiedName) {
                    Func<MethodBase, MethodInfo> getMethod =
                        mi => mi.ReflectedType.GetMethod(mi.Name,
                                                         BindingFlags.Instance
                                                         | BindingFlags.Static
                                                         | BindingFlags.Public
                                                         | BindingFlags.NonPublic
                                                         | BindingFlags.DeclaredOnly,
                                                         null,
                                                         mi.GetParameters()
                                                             .Select(pi => pi.ParameterType)
                                                             .ToArray(),
                                                         null);

                    var calling = getMethod(callingMethod);
                    var called  = getMethod(calledMethod);
                    return (!(calling == null || called == null)
                            && calling == called);
                } */
            }

            return false;
        }
    }
}
