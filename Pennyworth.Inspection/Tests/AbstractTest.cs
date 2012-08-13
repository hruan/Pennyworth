using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Pennyworth.Inspection.Tests {
	internal abstract class AbstractTest {
		private readonly Assembly         _assembly;
		private readonly List<MemberInfo> _faults;

		// Assemblies are shadow copied when loaded and we still need to tell the location
		// of the assembly being tested
		private readonly String _assemblyLocation;

		protected AbstractTest(Assembly assembly, String path) {
			_assembly         = assembly;
			_faults           = new List<MemberInfo>();
			_assemblyLocation = path;
		}

		protected Assembly Assembly {
			get { return _assembly; }
		}

		protected List<MemberInfo> Faults {
			get { return _faults; }
		}

		/// <summary>
		/// Executes the test case, <see cref="_faults"/> gets populated
		/// </summary>
		/// <remarks>
		/// Concrete test cases must make sure the populate <see cref="_faults"/> as
		/// <see cref="Runner"/> calls <see cref="HasFaults"/> and <see cref="Faults"/>
		/// to query and retrieve the results respectively.
		/// </remarks>
		/// <returns><c>true</c> if tests ran successfully; <c>false</c> otherwise</returns>
		internal abstract Boolean Run();

		/// <summary>
		/// Returns the faults detected by a test case
		/// </summary>
		/// <remarks>
		/// As the results will move across AppDomains they need to serializable objects.
		/// Assembly under test is only loaded in this AppDomain.
		/// </remarks>
		/// <seealso cref="Extensions.ToFaultInfo"/>.
		internal IEnumerable<FaultInfo> GetFaults() {
			var caseName = Attribute.GetCustomAttribute(GetType(), typeof(TestCaseAttribute)) as TestCaseAttribute;

			return _faults.ToFaultInfo(caseName != null ? caseName.Name : String.Empty, _assemblyLocation);
		}

		/// <summary>
		/// Whether the test case found any faults
		/// </summary>
		internal Boolean HasFaults {
			get { return _faults.Any(); }
		}
	}

	/// <summary>
	/// Used to mark test cases and allow for a more friendly test case name
	/// </summary>
	[Serializable]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	internal sealed class TestCaseAttribute : Attribute {
		private readonly String _name;

		public String Name {
			get { return _name; }
		}

		public TestCaseAttribute(String name) {
			_name = name;
		}
	}

	internal static class Extensions {
		/// <summary>
		/// Convert faulty member infos to <see cref="FaultInfo">FaultInfo</see>
		/// </summary>
		/// <remarks>
		/// This needs to be done as results move across AppDomains.
		/// </remarks>
		internal static IEnumerable<FaultInfo> ToFaultInfo(this IEnumerable<MemberInfo> memberInfos, String testName, String location) {
			return memberInfos.Select(mi => new FaultInfo {
				FaultType     = testName,
				Name          = mi.Name,
				MemberType    = mi.MemberType.ToString(),
				DeclaringType = mi.DeclaringType.ToString(),
				Path          = location
			}).ToList();
		}
	}
}
