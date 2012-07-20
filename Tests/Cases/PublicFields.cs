using System;
using System.Linq;
using System.Reflection;

namespace Tests.Cases {
    [TestCase("Public field")]
    public sealed class PublicFields : AbstractTest {
        public PublicFields(Assembly assembly, String path)
            : base(assembly, path) {}

        public override Boolean Run() {
            _faultyMembers.AddRange(_assembly.GetTypes()
                                        .Where(t => !t.IsNested)
                                        .SelectMany(t => t.GetFields(BindingFlags.Instance
                                                                     | BindingFlags.Public
                                                                     | BindingFlags.DeclaredOnly))
                                        // Apparently, enums have a special public field named value__
                                        .Where(fi => fi.DeclaringType != null
                                                     && !fi.DeclaringType.IsEnum));

	        return true;
        }
    }
}
