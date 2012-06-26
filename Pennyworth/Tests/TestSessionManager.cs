using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NLog;

namespace Pennyworth.Tests {
    public sealed class TestSessionManager : IDisposable {
        private AppDomain _appDomain;
        private Boolean _hasBeenUnloaded;
        private readonly AssemblyRegistry<Guid, String> _assemblyRegistry;
        private readonly Logger _logger;

        public TestSessionManager() {
            Faults = new List<FaultInfo>();
            _logger = LogManager.GetLogger(GetType().Name);
            _assemblyRegistry = new AssemblyRegistry<Guid, String>();
        }

        public List<FaultInfo> Faults { get; private set; }

        public Boolean RunTestsFor(String basePath, IEnumerable<String> paths) {
            Debug.Assert(!String.IsNullOrEmpty(basePath) && paths != null);

            const String cacheDirName = "Cache";
            var cacheDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                               + Path.DirectorySeparatorChar + cacheDirName;

            _appDomain = AppDomain.CreateDomain(Path.GetDirectoryName(basePath) ?? "worker",
                                                null,
                                                new AppDomainSetup {
                                                    CachePath = cacheDirPath,
                                                    DisallowCodeDownload = true,
                                                    ShadowCopyFiles = "true"
                                                });

            _logger.Debug("Created worker AppDomain.");

            if (paths.Any(path => !RunTestsFor(path))) {
                _logger.Error("Halting tests.");
                return false;
            }

            var dups = _assemblyRegistry.Duplicates();
            if (dups.Any()) {
                _logger.Warn("There were some duplicate assemblies found.  Only the first one was tested.");

                var sb = new StringBuilder();
                var @base = new Uri(basePath);
                foreach (var dup in dups) {
                    sb.Clear();
                    for (var i = 0; i < dup.Count; i++) {
                        sb.AppendFormat("{{{0}}} ", i);
                    }

                    sb.Append("are duplicates");
                    _logger.Warn(sb.ToString(),
                                 dup.Select(d => @base.MakeRelativeUri(new Uri(d))
                                                      .ToString()
                                                      .Replace(Path.AltDirectorySeparatorChar,
                                                               Path.DirectorySeparatorChar))
                                    .Cast<Object>()
                                    .ToArray());
                }
            }

            return true;
        }

        public void Dispose() {
            if (!_hasBeenUnloaded) {
                AppDomain.Unload(_appDomain);
                _hasBeenUnloaded = true;
                _logger.Debug("Worker domain unloaded.");
            }
        }

        private Boolean RunTestsFor(String path) {
            Debug.Assert(path != null);

            _logger.Info("Testing {0}", path);
            TestSession session = null;
            try {
                session = (TestSession) _appDomain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location,
                                                                               typeof(TestSession).FullName,
                                                                               ignoreCase:  false,
                                                                               bindingAttr: BindingFlags.Default,
                                                                               binder:      null,
                                                                               args:        new Object[] { path, _assemblyRegistry },
                                                                               culture:     null,
                                                                               activationAttributes: null);
            } catch (TargetInvocationException ex) {
                _logger.Error("Couldn't instantiate tester: {0}", ex.InnerException.Message);
            }

            if (session != null) {
                session.RunTests();
                if (session.HasFaults()) Faults.AddRange(session.GetFaults());
            }

            return session != null;
        }
    }

    public sealed class AssemblyRegistry<TKey, TElem> : MarshalByRefObject {
        private readonly Dictionary<TKey, List<TElem>> _registry;

        public AssemblyRegistry() {
            _registry = new Dictionary<TKey, List<TElem>>();
        }

        public Boolean Register(TKey key, TElem elem) {
            if (_registry.ContainsKey(key)) {
                _registry[key].Add(elem);
                return false;
            }
            
            _registry[key] = new List<TElem> { elem };
            return true;
        }

        public IList<List<TElem>> Duplicates() {
            return _registry.Where(kvp => kvp.Value.Count > 1)
                .Select(kvp => kvp.Value)
                .ToList();
        }
    }

    [Serializable]
    public struct FaultInfo {
        public String MemberType { get; set; }
        public String Path { get; set; }
        public String Name { get; set; }
        public String DeclaringType { get; set; }
    }
}
