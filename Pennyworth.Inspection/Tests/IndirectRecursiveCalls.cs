using System;
using System.Reflection;

namespace Pennyworth.Inspection.Tests {
	[TestCase("Indirect recursion")]
	internal sealed class IndirectRecursiveCalls : AbstractTest {
		internal IndirectRecursiveCalls(Assembly assembly, string path)
			: base(assembly, path) {}

		public override Boolean Run() {
			var faults = new MethodCallHelper(Assembly).GetIndirectRecursiveCalls();
			foreach (var fault in faults) {
				Faults.Add(fault);
			}

			return true;
		}
	}
}
