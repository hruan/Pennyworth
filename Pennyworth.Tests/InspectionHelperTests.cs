using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Pennyworth.Inspection;
using Ploeh.AutoFixture;

namespace Pennyworth.Tests {
	[TestFixture]
	public class InspectionHelperTests {
		private readonly IFixture _fixture = new Fixture().Customize(new MultipleCustomization());
        private readonly Assembly _stub = StubAssemblyBuilder.Create(new Fixture().CreateAnonymous<String>());

		private InspectionHelper _sut;

		[SetUp]
		public void Setup()
		{
			_fixture.Register<InspectionHelper>(() =>
				new InspectionHelper(_stub));
			_sut = _fixture.CreateAnonymous<InspectionHelper>();
		}

		[Test]
		public void GetRecursiveCalls_ReturnsOneFault()
		{
			Assert.AreEqual(1, _sut.GetRecursiveCalls().Count());
		}

        [Test]
        public void GetIndirectCalls_ReturnsTwoFaults()
        {
            Assert.AreEqual(2, _sut.GetIndirectRecursiveCalls().Count());
        }
	}
}
