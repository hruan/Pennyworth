using System;
using System.Collections.Generic;

namespace Pennyworth.Inspection {
	public interface ISession : IDisposable {
		IEnumerable<AssemblyInfo> PreparedAssemblies { get; }

		void Add(IEnumerable<String> paths);
		Boolean Remove(IEnumerable<AssemblyInfo> assemblies);

		IEnumerable<FaultInfo> Inspect();
	}
}
