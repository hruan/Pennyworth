using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tests {
    public abstract class AbstractTest {
        protected readonly Assembly _assembly;
        protected readonly List<MemberInfo> _faultyMembers;

        // Assemblies are shadow copied when loaded and we still need to tell the location
        // of the assembly being tested
        private readonly String _assemblyLocation;

        protected AbstractTest(Assembly assembly, String path) {
            _assembly = assembly;
            _faultyMembers = new List<MemberInfo>();
            _assemblyLocation = path;
        }

        /// <summary>
        /// Executes the test case, <see cref="_faultyMembers"/> gets populated
        /// </summary>
        /// <remarks>
        /// Concrete test cases must make sure the populate <see cref="_faultyMembers"/> as
        /// <see cref="TestSession"/> calls <see cref="HasFaults"/> and <see cref="Faults"/>
        /// to query and retrieve the results respectively.
        /// </remarks>
        public abstract void Run();

        /// <summary>
        /// Returns the faults detected by a test case
        /// </summary>
        /// <remarks>
        /// As the results will move across AppDomains they need to serializable objects.
        /// Assembly under test is only loaded in this AppDomain.
        /// </remarks>
        /// <seealso cref="Extensions.ToFaultInfo"/>.
        public IEnumerable<FaultInfo> GetFaults() {
            var caseName = Attribute.GetCustomAttribute(GetType(), typeof(TestCaseAttribute)) as TestCaseAttribute;

            return _faultyMembers.ToFaultInfo(caseName != null ? caseName.Name : String.Empty, _assemblyLocation);
        }

        /// <summary>
        /// Whether the test case found any faults
        /// </summary>
        public Boolean HasFaults {
            get { return _faultyMembers.Any(); }
        }
    }

    /// <summary>
    /// Used to mark test cases and allow for a more friendly test case name
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TestCaseAttribute : Attribute {
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
