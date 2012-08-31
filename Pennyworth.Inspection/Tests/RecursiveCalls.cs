using System;
using System.Reflection;

namespace Pennyworth.Inspection.Tests {
	[TestCase("Recursion")]
	internal sealed class RecursiveCalls : AbstractTest {
		internal RecursiveCalls(Assembly assembly, String path)
			: base(assembly, path) { }

		public override Boolean Run()
		{
			try {
				foreach (var fault in _helper.GetRecursiveCalls()) {
					Faults.Add(fault);
				}
			} catch (NotSupportedException) {
				return false;
			}

			return true;
		}
	}
}
