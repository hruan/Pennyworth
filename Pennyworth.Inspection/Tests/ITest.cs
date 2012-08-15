using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pennyworth.Inspection.Tests {
	public interface ITest {
		Boolean Run();
		Boolean HasFaults { get; }
		IEnumerable<FaultInfo> GetFaults();
	}
}
