using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NLog;

namespace Pennyworth.Inspection {
	/// <summary>
	/// A session consisting of a set of assemblies that need to be tested. Uses
	/// <see cref="Runner"/> to test each assembly.
	/// </summary>
	/// <remarks>
	/// Assemblies are loaded into a separate AppDomain which is unloaded after all
	/// tests have concluded.
	/// </remarks>
	public sealed class Session : ISession {
		private AppDomain _appDomain;

		private readonly Logger                  _log;
		private readonly String                  _basePath;
		private readonly ICollection<FaultInfo>  _faults;
		private readonly ICollection<RunnerInfo> _runners;

		public Session(String basePath)
		{
			Debug.Assert(!String.IsNullOrEmpty(basePath));

			_basePath = basePath;
			_runners = new List<RunnerInfo>();
			_faults = new List<FaultInfo>();
			_log = LogManager.GetLogger(GetType().Name);

			var cacheDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
			                   + Path.DirectorySeparatorChar + "Cache";

			CreateWorkerDomain(Path.GetDirectoryName(_basePath), cacheDirPath);
		}

		public void Add(IEnumerable<String> paths)
		{
			Debug.Assert(paths != null);

			foreach (var assembly in paths) {
				var runner = CreateRunner(assembly);
				_runners.Add(new RunnerInfo {
					AssemblyInfo = runner.AssemblyInfo,
					Runner = runner
				});
			}
		}

		public Boolean Remove(IEnumerable<AssemblyInfo> assemblies)
		{
			Debug.Assert(assemblies != null);

			var dupes = assemblies.Select(assembly =>
			                              _runners.First(x =>
			                                             x.AssemblyInfo.Path.Equals(assembly.Path,
				                                             StringComparison.OrdinalIgnoreCase)
			                                             && x.AssemblyInfo.Equals(assembly)
				                              )
				);

			return dupes.All(x => _runners.Remove(x));
		}

		public IEnumerable<FaultInfo> Inspect()
		{
			var tasks = new List<Task<Boolean>>(_runners.Count);
			tasks.AddRange(_runners.Select(x => Task.Factory.StartNew(() => x.Runner.RunTests())));
			Task.WaitAll(tasks.Cast<Task>().ToArray());

			_runners.Where(r => r.Runner.HasFaults)
				.SelectMany(x => x.Runner.GetFaults())
				.ToList()
				.ForEach(x => _faults.Add(x));

			return _faults;
		}

		public IEnumerable<AssemblyInfo> PreparedAssemblies
		{
			get { return _runners.Select(x => x.AssemblyInfo); }
		}

		public void Dispose()
		{
			if (_appDomain != null) {
				AppDomain.Unload(_appDomain);
				_log.Debug("Worker domain unloaded.");
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
		private void CreateWorkerDomain(String name, String cachePath)
		{
			_appDomain = AppDomain.CreateDomain(name,
				null,
				new AppDomainSetup {
					CachePath = cachePath,
					DisallowCodeDownload = true,
					ShadowCopyFiles = "true"
				});
			_log.Debug("Created worker AppDomain.");
		}

		/// <summary>
		/// Instantiate <see cref="Runner"/> in the session's AppDomain
		/// </summary>
		/// <param name="path">path to assembly to test</param>
		/// <returns>instante of <see cref="Runner"/>; null if instantiation failed</returns>
		private Runner CreateRunner(String path)
		{
			Runner runner = null;

			try {
				runner = (Runner) _appDomain.CreateInstanceFromAndUnwrap(
					Assembly.GetExecutingAssembly().Location,
					typeof(Runner).FullName,
					false,
					BindingFlags.Default,
					null,
					new Object[] { path },
					null,
					null);
			} catch (TargetInvocationException ex) {
				_log.Error("Couldn't instantiate test runner: {0}", ex.InnerException.Message);
			}

			return runner;
		}
	}

	[Serializable]
	public class RunnerInfo {
		public AssemblyInfo AssemblyInfo { get; set; }
		public Runner       Runner       { get; set; }
	}

	/// <summary>
	/// Used to populate the list of results and getting data across AppDomains
	/// </summary>
	[Serializable]
	public class FaultInfo {
		public String FaultType     { get; set; }
		public String MemberType    { get; set; }
		public String Path          { get; set; }
		public String Name          { get; set; }
		public String DeclaringType { get; set; }
	}
}
