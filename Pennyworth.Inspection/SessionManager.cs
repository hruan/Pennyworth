using System;
using System.Linq;
using System.Collections.Generic;
using NLog;
using System.Collections.ObjectModel;

namespace Pennyworth.Inspection {
	public class SessionManager {
		private readonly ICollection<FaultInfo> _faults;
		private readonly ISet<AssemblyInfo> _registry;
		private readonly Logger _log;

		private ISession _currentSession;

		public ICollection<FaultInfo> Faults
		{
			get { return _faults; }
		}

		internal ISession CurrentSession
		{
			get { return _currentSession; }
			set { _currentSession = value; }
		}

		public SessionManager()
		{
			_faults = new ObservableCollection<FaultInfo>();
			_registry = new HashSet<AssemblyInfo>();
			_log = LogManager.GetLogger(typeof(SessionManager).Name);
		}

		public IDisposable CreateSession(String basePath)
		{
			_currentSession = new Session(basePath);

			return _currentSession;
		}

		public void Add(IEnumerable<String> assemblies)
		{
			_currentSession.Add(assemblies);
		}

		public Boolean RunTests()
		{
			var dupes = _currentSession.PreparedAssemblies
				.Where(x => !_registry.Add(x))
				.ToList();
			Discard(dupes);

			// Run tests and grab detected faults
			_currentSession.Inspect()
				.ToList()
				.ForEach(x => _faults.Add(x));

			return _faults.Count <= 0;
		}

		private void Discard(IEnumerable<AssemblyInfo> ignored)
		{
			_currentSession.Remove(ignored);
			foreach (var assembly in ignored) {
				_log.Warn("{0} is a duplicate and is not being tested.", assembly.Path);
			}
		}
	}
}
