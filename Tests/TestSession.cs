using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Tests {
    /// <summary>
    /// Loads an assembly and constructs are test session around it. Session
    /// can be controlled across AppDomains.
    /// </summary>
    public sealed class TestSession : MarshalByRefObject {
        private readonly Assembly _assembly;
        private readonly String _path;
        private readonly ICollection<AbstractTest> _preparedTests;
        private readonly AssemblyRegistry<Guid, String> _registry;

        private static readonly List<Type> _testCases;

        /// <summary>
        /// Find tests through reflection when type is loaded
        /// </summary>
        static TestSession() {
            var baseType = typeof(AbstractTest);

            _testCases = Assembly.GetExecutingAssembly()
                .GetExportedTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && baseType.IsAssignableFrom(t)
                            && Attribute.GetCustomAttribute(t, typeof(TestCaseAttribute), false) != null)
                .ToList();
        }

        /// <summary>
        /// Construct a session for a given assembly. Also attempts to register
        /// the assembly identified by the manifest module's UUID; tests are
        /// only run if registration succeeds.
        /// </summary>
        /// <param name="path">path to the assembly to test</param>
        /// <param name="registry">registry where assembly is to be registered</param>
        public TestSession(String path, AssemblyRegistry<Guid, String> registry) {
            try {
                _path          = path;
                _assembly      = Assembly.LoadFrom(path);
                _preparedTests = PrepareTests().ToList();
                _registry      = registry;

            } catch (ArgumentException argumentException) {
                Debug.WriteLine(argumentException.ToString());
            } catch (IOException ioException) {
                Debug.WriteLine(ioException.ToString());
            }
        }

        /// <summary>
        /// Execute prepared tests on given assembly
        /// </summary>
        public void RunTests() {
            Debug.Assert(_preparedTests != null);

            if (_registry.Register(CurrentAssemblyGuid, _path)) {
                foreach (var test in _preparedTests) {
                    test.Run();
                } 
            }
        }

        public Boolean HasFaults {
            get { return _preparedTests.Any(t => t.HasFaults); }
        }

        /// <summary>
        /// Gets the faults from executed tests; results are flattened
        /// </summary>
        /// <returns>flattened list of faults from all the tests; empty list if no faults were found</returns>
        public IEnumerable<FaultInfo> GetFaults() {
            return HasFaults
                       ? _preparedTests.SelectMany(t => t.GetFaults()).ToList()
                       : Enumerable.Empty<FaultInfo>().ToList();
        }

        /// <summary>
        /// Identifies the assembly through the UUID of the module containing
        /// the assembly manifest.
        /// </summary>
        private Guid CurrentAssemblyGuid {
            get { return _assembly.ManifestModule.ModuleVersionId; }
        }

        /// <summary>
        /// Instantiate all found cases
        /// </summary>
        private IEnumerable<AbstractTest> PrepareTests() {
            return _testCases.Select(type => Activator.CreateInstance(type, _assembly, _path) as AbstractTest);
        }
    }
}
