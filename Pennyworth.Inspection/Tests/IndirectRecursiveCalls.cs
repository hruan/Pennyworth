using System;
using System.Reflection;

namespace Pennyworth.Inspection.Tests {
	[TestCase("Indirect recursion")]
	internal sealed class IndirectRecursiveCalls : AbstractTest {
		internal IndirectRecursiveCalls(Assembly assembly, string path)
			: base(assembly, path) { }

		public override Boolean Run()
		{
			try {
				foreach (var fault in _helper.GetIndirectRecursiveCalls()) {
					Faults.Add(fault);
				}
			} catch (NotSupportedException) {
				return false;
			}

			return true;
		}
	}
}
