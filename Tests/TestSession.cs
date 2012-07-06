using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Tests {
    public sealed class TestSession : MarshalByRefObject {
        private readonly Assembly _assembly;
        private readonly String _path;
        private readonly ICollection<AbstractTest> _tests;
        private readonly AssemblyRegistry<Guid, String> _registry;

        private static readonly ICollection<Type> _testTypes;

        static TestSession() {
            _testTypes = FindTests().ToList();
        }

        public TestSession(String path, AssemblyRegistry<Guid, String> registry) {
            try {
                _path = path;
                _assembly = Assembly.LoadFrom(path);
                _tests = PrepareTests().ToList();
                _registry = registry;

            } catch (ArgumentException argumentException) {
                Debug.WriteLine(argumentException.ToString());
            } catch (IOException ioException) {
                Debug.WriteLine(ioException.ToString());
            }
        }

        public void RunTests() {
            Debug.Assert(_tests != null);

            if (_registry.Register(CurrentAssemblyGuid, _path)) {
                foreach (var test in _tests) {
                    test.Run();
                } 
            }
        }

        public Boolean HasFaults() {
            return _tests.Any(t => t.HasFaults);
        }

        public IEnumerable<FaultInfo> GetFaults() {
            return HasFaults()
                       ? _tests.SelectMany(t => t.GetFaults()).ToList()
                       : Enumerable.Empty<FaultInfo>().ToList();
        }

        private Guid CurrentAssemblyGuid {
            get { return _assembly.ManifestModule.ModuleVersionId; }
        }

        private IEnumerable<AbstractTest> PrepareTests() {
            return _testTypes.Select(type => Activator.CreateInstance(type, _assembly, _path) as AbstractTest);
        }

        private static IEnumerable<Type> FindTests() {
            var baseType = typeof(AbstractTest);

            return Assembly.GetExecutingAssembly()
                .GetExportedTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && baseType.IsAssignableFrom(t)
                            && Attribute.GetCustomAttribute(t, typeof(TestCaseAttribute), false) != null);
        }
    }
}
