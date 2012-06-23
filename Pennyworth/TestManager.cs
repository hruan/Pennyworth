using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Pennyworth {
    public class TestManager : MarshalByRefObject {
        private Assembly _currentAssembly;

        public void LoanAssembly(String path) {
            _currentAssembly = Assembly.LoadFrom(path);
        }

        public Boolean AssemblyHasPublicFields(String path) {
            var at = new AssemblyTest(path);
            return at.HasPublicFields();
        }
    }

    public static class AppDomainHelper {
        private static readonly List<AppDomain> _domains;

        static AppDomainHelper() {
            _domains = new List<AppDomain>();
        }

        public static AppDomain CreateDomain(String name) {
            return AppDomain.CreateDomain(name);
        }

        public static Boolean UnloadDomain(AppDomain domain) {
            var result = _domains.Remove(domain);
            if (result) {
                AppDomain.Unload(domain);
            }

            return result;
        }
    }
}
