using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Pennyworth {
    public sealed class AssemblyTestRunner : IDisposable {
        private AppDomain _appDomain;
        private Boolean _hasBeenUnloaded;

        public IEnumerable<OffendingMember> RunTestsFor(String path) {
            Debug.Assert(path != null);

            const String cacheDirName = "Cache";
            var cacheDirPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + cacheDirName;

            _appDomain = AppDomain.CreateDomain(Path.GetFileNameWithoutExtension(path), null, new AppDomainSetup {
                CachePath = cacheDirPath,
                DisallowCodeDownload = true,
                ShadowCopyFiles = "true"
            });

            var offendingItems = new List<OffendingMember>();
            var tester = (AssemblyTest) _appDomain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location,
                                                                               typeof(AssemblyTest).FullName,
                                                                               ignoreCase: false,
                                                                               bindingAttr: BindingFlags.Default,
                                                                               binder: null,
                                                                               args: new[] {path},
                                                                               culture: null,
                                                                               activationAttributes: null);
            // Log(String.Format("Peeking inside {0}", currentFile));

            if (tester.HasPublicFields()) {
                offendingItems.AddRange(tester.GetPublicFields());
            }

            if (tester.GetRecursiveMembers().Any()) {
                offendingItems.AddRange(tester.GetRecursiveMembers());
            }

            return offendingItems;
        }

        public void Dispose() {
            if (!_hasBeenUnloaded) {
                Debug.Print("Unloading {0}", _appDomain.FriendlyName);
                AppDomain.Unload(_appDomain);
                _hasBeenUnloaded = true;
            }
        }
    }
}
