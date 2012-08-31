using System;
using System.Linq;
using System.Reflection;

namespace Pennyworth.Inspection.Tests {
	[TestCase("Public field")]
	internal sealed class PublicFields : AbstractTest {
		internal PublicFields(Assembly assembly, String path)
			: base(assembly, path) { }

		public override Boolean Run()
		{
			foreach (var fault in _helper.GetPublicFields()) {
				Faults.Add(fault);
			}

			return true;
		}
	}
}
