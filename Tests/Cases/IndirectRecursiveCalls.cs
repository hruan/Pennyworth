using System.Reflection;

namespace Tests.Cases {
    [TestCase("Indirect recursion")]
    public sealed class IndirectRecursiveCalls : AbstractTest {
        public IndirectRecursiveCalls(Assembly assembly, string path)
            : base(assembly, path) {}

        public override void Run() {
            _faultyMembers.AddRange(new MethodCallHelper(_assembly).GetIndirectRecursiveCalls());
        }
    }
}
