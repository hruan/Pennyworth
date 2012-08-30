using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pennyworth.Inspection.Tests;

namespace Pennyworth.Inspection {
	public interface IRunner {
		void AddTest(ITest test);
		Boolean Run();
	}
}
