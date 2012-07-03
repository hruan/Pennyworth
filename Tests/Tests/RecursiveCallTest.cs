using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace TestLibrary.Tests {
    [Test]
    public sealed class RecursiveCallTest : AbstractTest {
        private readonly Dictionary<MethodInfo, Byte[]> _methodByteCode;
        private static readonly Dictionary<Int16, OpCode> _opcodes;

        public RecursiveCallTest(Assembly assembly, String path)
            : base(assembly, path) {
            _methodByteCode = _assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Instance
                                              | BindingFlags.NonPublic
                                              | BindingFlags.Public
                                              | BindingFlags.Static
                                              | BindingFlags.DeclaredOnly))
                .Where(mi => mi.GetMethodBody() != null)
                .ToDictionary(mi => mi,
                              mi => mi.GetMethodBody().GetILAsByteArray());
        }

        static RecursiveCallTest() {
            _opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(fieldInfo => fieldInfo.GetValue(null))
                .Cast<OpCode>()
                .ToDictionary(opcode => opcode.Value);
        }

        public override void Run() {
            foreach (var kvp in _methodByteCode) {
                var offset = 0;
                var byteCodes = kvp.Value;

                while (offset < byteCodes.Length) {
                    Int16 opcode = byteCodes[offset];

                    // Multibyte opcode?
                    // http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf
                    // Partition III, Table 1
                    if ((opcode & 0xfe) == 0) {
                        opcode = (Int16) (opcode << 8 | byteCodes[offset + 1]);
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
                                try {
                                    operandSize = checked(BitConverter.ToInt32(byteCodes, offset + instruction.Size) * 4);
                                } catch (OverflowException ex) {
                                    operandSize = 0;
                                }
                                break;

                            case OperandType.InlineMethod:
                                if (instruction.FlowControl == FlowControl.Call) {
                                    var operand       = BitConverter.ToInt32(byteCodes, offset + instruction.Size);
                                    var callingMethod = kvp.Key.GetBaseDefinition();

                                    if (IsRecursiveCall(callingMethod, operand)) _faultyMembers.Add(kvp.Key);
                                }

                                break;
                        }

                        offset += _opcodes[opcode].Size + operandSize;
                    } else {
                        offset += (opcode & 0xff00) == 0 ? 1 : 2;
                    }
                }
            }
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
                    // Debug.Print("Couldn't resolve method call in {0}: {1}", callingMethod.Name, ex.Message);
                }

                return calledMethod != null
                       && callingMethod == calledMethod;
            }

            return false;
        }
    }
}
