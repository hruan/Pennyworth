using System;
using System.Reflection;

namespace Pennyworth.Inspection.Tests {
	[TestCase("Recursion")]
	internal sealed class RecursiveCalls : AbstractTest {
		internal RecursiveCalls(Assembly assembly, String path)
			: base(assembly, path) { }

		internal override Boolean Run() {
			try {
				var faults = new MethodCallHelper(Assembly).GetRecursiveCalls();
				foreach (var fault in faults) {
					Faults.Add(fault);
				}
			} catch (NotSupportedException) {
				return false;
			}

			return true;
		}
	}
}
