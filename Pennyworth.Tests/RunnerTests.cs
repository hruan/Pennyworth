using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Pennyworth.Inspection;
using Pennyworth.Inspection.Tests;
using Ploeh.AutoFixture;

namespace Pennyworth.Tests {
	[TestFixture]
	public class RunnerTests {
		private readonly IFixture _fixture = new Fixture().Customize(new MultipleCustomization());

		private Runner _sut;

		[SetUp]
		public void Setup()
		{
			var t = Substitute.For<ITest>();
			_sut = _fixture.CreateAnonymous<Runner>();
			_sut.Tests = new List<ITest>();

			_sut.AddTest(t);
			_sut.AddTest(t);
			_sut.AddTest(t);
		}

		[Test]
		public void RunTests_CallsRunOnAllPreparedTests()
		{
			_sut.Run();

			_sut.Tests.All(x => x.Received(1).Run());
		}

		[Test]
		public void RunTests_FailedTestsReturnsFalse_OtherwiseTrue(
			[Values(false, true)] Boolean returnValue)
		{
			_sut.Tests.First().Run().Returns(returnValue);

			Assert.AreEqual(returnValue, _sut.Run());
		}

		[Test]
		public void HasFaults_ReturnsTrueIfTestsFoundFaults_OtherwiseFalse(
			[Values(false, true)] Boolean returnValue)
		{
			_sut.Tests.First().HasFaults.Returns(returnValue);

			Assert.AreEqual(returnValue, _sut.HasFaults);
		}

		[Test]
		public void HasFaults_CallsHasFaultsOnAtleastOneTest()
		{
			var _ =_sut.HasFaults;

			_sut.Tests.Any(x => x.Received().HasFaults);
		}

		[Test]
		public void HasFaults_ReturnsTheRightNumberOfFaults(
			[Values(0, 3)] Int32 expectedFaults)
		{
			var faults = expectedFaults > 0
				? _fixture.CreateMany<FaultInfo>(expectedFaults).ToList()
				: Enumerable.Empty<FaultInfo>();

			_sut.HasFaults.Returns(expectedFaults != 0);
			_sut.Tests.First().GetFaults().Returns(faults);

			Assert.AreEqual(expectedFaults * _sut.Tests.Count, _sut.GetFaults().Count());
		}
	}
}
