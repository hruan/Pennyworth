using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pennyworth.Tests {
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
            get { return _faultyMembers.ToFaultInfo(_assemblyLocation); }
        }

        public Boolean HasFaults() {
            return _faultyMembers.Any();
        }
    }

    [Serializable]
    public class TestAttribute : Attribute {
    }

    internal static class Extensions {
        internal static IEnumerable<FaultInfo> ToFaultInfo(this IEnumerable<MemberInfo> memberInfos, String location) {
            return memberInfos.Select(mi => new FaultInfo {
                Name          = mi.Name,
                MemberType    = mi.MemberType.ToString(),
                DeclaringType = mi.DeclaringType.ToString(),
                Path          = location
            }).ToList();
        }
    }
}
