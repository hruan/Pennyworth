using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;

namespace Pennyworth {
    public sealed class AssemblyTestRunner : IDisposable {
        private AppDomain _appDomain;
        private Boolean _hasBeenUnloaded;
        private readonly Logger _logger;

        public AssemblyTestRunner() {
            Offences = new List<OffendingMember>();
            _logger = LogManager.GetLogger(GetType().Name);
        }

        public List<OffendingMember> Offences { get; private set; }

        public Boolean RunTestsFor(IEnumerable<String> paths) {
            Debug.Assert(paths != null);

            const String cacheDirName = "Cache";
            var cacheDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                               + Path.DirectorySeparatorChar + cacheDirName;

            _appDomain = AppDomain.CreateDomain("worker", null, new AppDomainSetup {
                CachePath            = cacheDirPath,
                DisallowCodeDownload = true,
                ShadowCopyFiles      = "true"
            });

            _logger.Debug("Created worker AppDomain.");
            if (paths.Any(path => !RunTestsFor(path))) {
                _logger.Warn("Halting tests.");
                return false;
            }

            return true;
        }

        private Boolean RunTestsFor(String path) {
            Debug.Assert(path != null);

            _logger.Info("Testing {0}", path);
            AssemblyTest tester = null;
            try {
                tester = (AssemblyTest) _appDomain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location,
                                                                               typeof(AssemblyTest).FullName,
                                                                               ignoreCase:  false,
                                                                               bindingAttr: BindingFlags.Default,
                                                                               binder:      null,
                                                                               args:        new[] {path},
                                                                               culture:     null,
                                                                               activationAttributes: null);
            } catch (TargetInvocationException ex) {
                _logger.Error("Couldn't instantiate tester: {0}", ex.InnerException.Message);
            }

            if (tester != null) {
                if (tester.HasPublicFields()) {
                    _logger.Info("{0} has public fields!", path);
                    Offences.AddRange(tester.GetPublicFields());
                }

                if (tester.GetRecursiveMembers().Any()) {
                    _logger.Info("{0} has recursive members!", path);
                    Offences.AddRange(tester.GetRecursiveMembers());
                }
            }

            return tester != null;
        }

        public void Dispose() {
            if (!_hasBeenUnloaded) {
                AppDomain.Unload(_appDomain);
                _hasBeenUnloaded = true;
                _logger.Debug("Worker domain unloaded.");
            }
        }
    }

    [Serializable]
    public struct OffendingMember {
        public String MemberType { get; set; }
        public String Path { get; set; }
        public String Name { get; set; }
        public String DeclaringType { get; set; }
    }
}
