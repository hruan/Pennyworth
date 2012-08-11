using System;
using System.Reflection;

namespace Pennyworth.Inspection.Tests {
	[TestCase("Indirect recursion")]
	public sealed class IndirectRecursiveCalls : AbstractTest {
		public IndirectRecursiveCalls(Assembly assembly, string path)
			: base(assembly, path) {}

		public override Boolean Run() {
			var faults = new MethodCallHelper(Assembly).GetIndirectRecursiveCalls();
			foreach (var fault in faults) {
				FaultyMembers.Add(fault);
			}

			return true;
		}
	}
}
