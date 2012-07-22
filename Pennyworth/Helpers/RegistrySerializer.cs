using System;
using System.IO;
using NLog;
using Tests;

namespace Pennyworth.Helpers {
	public static class RegistrySerializer {
	    private const String AssemblyRegistryPath = @"Pennyworth.dat";

		private static AssemblyRegistry _registry;
		private static readonly Logger  _logger;

		static RegistrySerializer() {
			_logger = LogManager.GetLogger("RegistrySerializer");
		}

		public static AssemblyRegistry GetRegistry() {
			try {
				_registry = SerializationHelper.Deserialize<AssemblyRegistry>(AssemblyRegistryPath,
				                                                             SerializationType.Binary);
			} catch (IOException ex) {
				_logger.Warn("Exception while loading assembly registry: {0}", ex.Message);
			} finally {
				_registry = _registry ?? new AssemblyRegistry();
			}

			return _registry;
		}

		public static Boolean SaveRegistry() {
			return SerializationHelper.Serialize(_registry, AssemblyRegistryPath, SerializationType.Binary);
		}
	}
}
