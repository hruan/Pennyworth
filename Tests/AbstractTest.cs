using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tests {
    public abstract class AbstractTest {
        protected readonly Assembly _assembly;
        protected readonly List<MemberInfo> _faultyMembers;

        private readonly String _assemblyLocation;

        protected AbstractTest(Assembly assembly, String path) {
            _assembly = assembly;
            _faultyMembers = new List<MemberInfo>();
            _assemblyLocation = path;
        }

        public abstract void Run();

        public IEnumerable<FaultInfo> Faults {
            get {
                var caseName = Attribute.GetCustomAttribute(GetType(), typeof(TestCaseAttribute)) as TestCaseAttribute;

                return _faultyMembers.ToFaultInfo(caseName != null ? caseName.Name : String.Empty, _assemblyLocation);
            }
        }

        public Boolean HasFaults() {
            return _faultyMembers.Any();
        }
    }

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
