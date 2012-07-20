using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Tests {
    /// <summary>
    /// Deals test case discovery through reflection, test execution and gathering
    /// results from executed tests.
    /// </summary>
    /// <remarks>
    /// Can be controlled across AppDomains.
    /// </remarks>
    public sealed class TestRunner : MarshalByRefObject {
        private readonly Assembly _assembly;
        private readonly String _path;
        private readonly List<AbstractTest> _preparedTests;

        private static readonly List<Type> _testCases;

        /// <summary>
        /// Find tests through reflection when type is loaded
        /// </summary>
        static TestRunner() {
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
        /// Construct a runner for a given assembly. Also attempts to register
        /// the assembly identified by the manifest module's UUID; tests are
        /// only run if registration succeeds.
        /// </summary>
        /// <param name="path">path to the assembly to test</param>
        public TestRunner(String path) {
            try {
                _path          = path;
                _assembly      = Assembly.LoadFrom(path);
                _preparedTests = PrepareTests().ToList();

            } catch (ArgumentException argumentException) {
                Debug.WriteLine(argumentException.ToString());
            } catch (IOException ioException) {
                Debug.WriteLine(ioException.ToString());
            }
        }

        /// <summary>
        /// Execute prepared tests on given assembly
        /// </summary>
        public Boolean RunTests() {
            Debug.Assert(_preparedTests != null);

			return _preparedTests.All(test => test.Run());
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
        internal Guid CurrentAssemblyGuid {
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
