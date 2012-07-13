using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;

namespace Tests {
	/// <summary>
	/// A session consisting of a set of assemblies that need to be tested. Uses
	/// <see cref="TestRunner"/> to test each assembly.
	/// </summary>
	/// <remarks>
	/// Assemblies are loaded into a separate AppDomain which is unloaded after all
	/// tests have concluded.
	/// </remarks>
	public sealed class TestSession : IDisposable {
		private AppDomain _appDomain;
		private readonly String _basePath;
		private readonly List<FaultInfo> _faults;
		private readonly AssemblyRegistry<Guid, String> _assemblyRegistry;
		private readonly Logger _logger;

		public TestSession(string basePath) {
			Debug.Assert(!String.IsNullOrEmpty(basePath));

			_faults = new List<FaultInfo>();
			_logger = LogManager.GetLogger(GetType().Name);
			_assemblyRegistry = new AssemblyRegistry<Guid, String>();

			var cacheDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
							   + Path.DirectorySeparatorChar + "Cache";

			_basePath = basePath;
			CreateWorkerDomain(Path.GetDirectoryName(_basePath), cacheDirPath);
		}

		public IEnumerable<FaultInfo> Faults { get { return _faults; } }

		/// <summary>
		/// Prepare and executes tests for each assemblies whose path is
		/// supplied.
		/// </summary>
		/// <param name="paths">paths of assemblies to test</param>
		/// <returns>true if all tests suceeded; false otherwise</returns>
		public Boolean RunTestsFor(IEnumerable<String> paths) {
			Debug.Assert(paths != null);

			if (paths.Any(path => !PerformTestsOn(path))) {
				_logger.Error("Halting tests.");
				return false;
			}

			if (HasDuplicates()) {
				var dups = _assemblyRegistry.Duplicates()
					.Select(dup => DuplicatesRelativeLocation(dup, new Uri(_basePath)));

				_logger.Warn("There were some duplicate assemblies found.  Only the first one was tested.");
				foreach (var dup in dups) {
					_logger.Warn("{0} are duplicates", dup.Aggregate((cur, next) => cur + ", " + next));
				}
			}

			return true;
		}

		/// <summary>
		/// Unload worker AppDomain when disposed
		/// </summary>
		public void Dispose() {
			if (_appDomain != null) {
				AppDomain.Unload(_appDomain);
				_logger.Debug("Worker domain unloaded.");
			}
		}

		/// <summary>
		/// Creates a new AppDomain within which to run the tests.
		/// Assemblies are shadow copied to a cache directory so it doesn't
		/// interrupt any recompilation that's done during the review
		/// process.
		/// </summary>
		/// <param name="name">friendly name of the AppDomain</param>
		/// <param name="cachePath">where to shadow copy assemblies</param>
		private void CreateWorkerDomain(String name, String cachePath) {
			_appDomain = AppDomain.CreateDomain(name,
												null,
												new AppDomainSetup {
													CachePath = cachePath,
													DisallowCodeDownload = true,
													ShadowCopyFiles = "true"
												});
			_logger.Debug("Created worker AppDomain.");
		}

		private Boolean HasDuplicates() {
			return _assemblyRegistry.Duplicates().Any();
		}

		/// <summary>
		/// Get paths to duplicates using relative path from <c>_basePath</c>
		/// </summary>
		/// <param name="duplicates">paths to duplicates</param>
		/// <param name="basePath">the <c>_basePath</c> as an URI</param>
		/// <returns></returns>
		private IEnumerable<String> DuplicatesRelativeLocation(IEnumerable<String> duplicates, Uri basePath) {
			return duplicates.Select(x => basePath.MakeRelativeUri(new Uri(x)).ToString())
				.Select(x => Uri.UnescapeDataString(x)
								.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
		}

		/// <summary>
		/// Instantiate <see cref="TestRunner"/> in worker AppDomain and use
		/// it to test an assembly.
		/// </summary>
		/// <param name="path">path to assembly to test</param>
		/// <returns>true if tests were performed correctly; false otherwise</returns>
		/// <remarks>
		/// Return value depends on whether tests were executed correctly and
		/// does not reflect whether any faults were detected.
		/// </remarks>
		private Boolean PerformTestsOn(String path) {
			Debug.Assert(path != null);

			_logger.Info("Testing {0}", path);
			TestRunner session = null;
			try {
				session = (TestRunner) _appDomain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location,
																			   typeof(TestRunner).FullName,
																			   ignoreCase:  false,
																			   bindingAttr: BindingFlags.Default,
																			   binder:      null,
																			   args:        new Object[] { path, _assemblyRegistry },
																			   culture:     null,
																			   activationAttributes: null);
			} catch (TargetInvocationException ex) {
				_logger.Error("Couldn't instantiate test runner: {0}", ex.InnerException.Message);
			}

			var testsRan = false;
			if (session != null) {
				testsRan = session.RunTests();
				if (testsRan && session.HasFaults) {
					_faults.AddRange(session.GetFaults());
				} else if (!testsRan) {
					_logger.Error("Some tests failed to run, see log file for details.");
				}
			}

			return session != null && testsRan;
		}
	}

	/// <summary>
	/// Used to populate the list of results and getting data across AppDomains
	/// </summary>
	[Serializable]
	public struct FaultInfo {
		public String FaultType { get; set; }
		public String MemberType { get; set; }
		public String Path { get; set; }
		public String Name { get; set; }
		public String DeclaringType { get; set; }
	}
}
