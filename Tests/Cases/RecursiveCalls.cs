using System;
using System.Reflection;

namespace Tests.Cases {
	[TestCase("Recursion")]
	public sealed class RecursiveCalls : AbstractTest {
		public RecursiveCalls(Assembly assembly, String path)
			: base(assembly, path) { }

		public override Boolean Run() {
			try {
				var faults = new MethodCallHelper(Assembly).GetRecursiveCalls();
				foreach (var fault in faults) {
					FaultyMembers.Add(fault);
				}
			} catch (NotSupportedException) {
				return false;
			}

			return true;
		}
	}
}
