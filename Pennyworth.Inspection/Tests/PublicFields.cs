using System;
using System.Linq;
using System.Reflection;

namespace Pennyworth.Inspection.Tests {
	[TestCase("Public field")]
	internal sealed class PublicFields : AbstractTest {
		internal PublicFields(Assembly assembly, String path)
			: base(assembly, path) {}

		public override Boolean Run() {
			var faults = Assembly.GetTypes()
				.Where(t => !t.IsNested)
				.SelectMany(t => t.GetFields(BindingFlags.Instance
											 | BindingFlags.Public
											 | BindingFlags.DeclaredOnly))
				// Apparently, enums have a special public field named value__
				.Where(fi => fi.DeclaringType != null
							 && !fi.DeclaringType.IsEnum);
			foreach (var fault in faults) {
				Faults.Add(fault);
			}

			return true;
		}
	}
}
