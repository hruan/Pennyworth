using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Pennyworth.Inspection.Tests;

namespace Pennyworth.Inspection {
	/// <summary>
	/// Deals test case discovery through reflection, test execution and gathering
	/// results from executed tests.
	/// </summary>
	/// <remarks>
	/// Can be controlled across AppDomains.
	/// </remarks>
	public sealed class Runner : MarshalByRefObject, IRunner {
		private Assembly _assembly;
		private ICollection<ITest> _preparedTests;

		private readonly String _path;

		private static readonly ICollection<Type> _testCases;

		internal Assembly Assembly
		{
			get { return _assembly; }
			set { _assembly = value; }
		}

		internal ICollection<ITest> Tests
		{
			get { return _preparedTests; }
			set { _preparedTests = value; }
		}

		/// <summary>
		/// Find tests through reflection when type is loaded
		/// </summary>
		static Runner()
		{
			var baseType = typeof(AbstractTest);

			_testCases = Assembly.GetExecutingAssembly()
				// Tests are internal -- can't use GetExportedTypes()
				.GetTypes()
				.Where(t => !t.IsPublic
				            && t.IsClass
				            && !t.IsAbstract
				            && baseType.IsAssignableFrom(t)
				            && Attribute.GetCustomAttribute(t, typeof(TestCaseAttribute), false) != null)
				.ToList();
		}

		/// <summary>
		/// Construct a runner for a given assembly. Also attempts to register
		/// the assembly identified by the manifest module's UUID; tests are
		/// only run if registration succeeds.
		/// </summary>
		/// <param name="path">path to the assembly to test</param>
		public Runner(String path)
		{
			try {
				_path = path;
				_assembly = Assembly.LoadFile(path);
				_preparedTests = PrepareTests().ToList();
			} catch (ArgumentException argumentException) {
				Debug.WriteLine(argumentException.ToString());
			} catch (IOException ioException) {
				Debug.WriteLine(ioException.ToString());
			}
		}

		/// <summary>
		/// Execute prepared tests on given assembly
		/// </summary>
		public Boolean RunTests()
		{
			Debug.Assert(_preparedTests != null);

			return _preparedTests.All(test => test.Run());
		}

		public Boolean HasFaults
		{
			get { return _preparedTests.Any(t => t.HasFaults); }
		}

		/// <summary>
		/// Gets the faults from executed tests; results are flattened
		/// </summary>
		/// <returns>flattened list of faults from all the tests; empty list if no faults were found</returns>
		public IEnumerable<FaultInfo> GetFaults()
		{
			return HasFaults
				       ? _preparedTests.SelectMany(t => t.GetFaults()).ToList()
				       : Enumerable.Empty<FaultInfo>().ToList();
		}

		public AssemblyInfo AssemblyInfo
		{
			get {
				var attr = Attribute.GetCustomAttribute(_assembly, typeof(GuidAttribute)) as GuidAttribute;

				return new AssemblyInfo {
					AssemblyId = _assembly.ManifestModule.ModuleVersionId,
					AssemblyGuid = attr != null ? new Guid(attr.Value) : Guid.Empty,
					Path = _path
				};
			}
		}

		public void AddTest(ITest test)
		{
			_preparedTests.Add(test);
		}

		/// <summary>
		/// Instantiate all found cases
		/// </summary>
		private IEnumerable<ITest> PrepareTests()
		{
			return _testCases.Select(type => Activator.CreateInstance(type,
				BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				new Object[] { _assembly, _path },
				CultureInfo.CurrentCulture) as AbstractTest);
		}
	}
}
