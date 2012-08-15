using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using NSubstitute;
using Pennyworth.Inspection;

namespace Pennyworth.Tests {
	[TestFixture]
	public class SessionManagerTests {
		private readonly IFixture _fixture = new Fixture().Customize(new MultipleCustomization());

		private SessionManager[] Session
		{
			get {
				return new[] {
					new SessionManager { CurrentSession = Substitute.For<ISession>() }
				};
			}
		}

		private IEnumerable<AssemblyInfo>[] AssemblyInfos
		{
			get {
				return new[] {
					_fixture.CreateMany<AssemblyInfo>().ToList()
				};
			}
		}

		[Test]
		[TestCaseSource("Session")]
		public void Add_WillAddAssemblyToSession(SessionManager sut)
		{
			var paths = _fixture.CreateMany<String>().ToList();

			sut.Add(paths);

			sut.CurrentSession.Received(1).Add(paths);
		}

		[Test]
		public void RunTests_WillDiscardDuplicateAssemblies(
			[ValueSource("Session")] SessionManager sut,
			[ValueSource("AssemblyInfos")] IEnumerable<AssemblyInfo> infos)
		{
			var session = sut.CurrentSession;

			session.PreparedAssemblies.Returns(infos.Concat(infos));
			sut.RunTests();

			sut.CurrentSession.Received().Remove(Arg.Is<IEnumerable<AssemblyInfo>>(x => x.SequenceEqual(infos)));
		}

		[Test]
		public void RunTests_RemoveNotCalledWhenNoDuplicates(
			[ValueSource("Session")] SessionManager sut,
			[ValueSource("AssemblyInfos")] IEnumerable<AssemblyInfo> infos)
		{
			var session = sut.CurrentSession;

			session.PreparedAssemblies.Returns(infos);
			sut.RunTests();

			sut.CurrentSession.DidNotReceive().Remove(Arg.Is<IEnumerable<AssemblyInfo>>(x => x.SequenceEqual(infos)));
		}

		[Test]
		[TestCaseSource("Session")]
		public void RunTests_InspectIsCalled(SessionManager sut)
		{
			sut.RunTests();

			sut.CurrentSession.Received(1).Inspect();
		}

		private IEnumerable FaultsSource
		{
			get {
				return new[] {
					new Object[] { Session.First(), false, _fixture.CreateMany<FaultInfo>() },
					new Object[] { Session.First(), true, Enumerable.Empty<FaultInfo>() }
				};
			}
		}

		[Test]
		[TestCaseSource("FaultsSource")]
		public void RunTests_ReturnsTrueWhenNoFaultsFound_OtherwiseFalse(
			SessionManager sut,
			Boolean expected,
			IEnumerable<FaultInfo> faults)
		{
			sut.CurrentSession.Inspect().Returns(faults);

			Assert.AreEqual(expected, sut.RunTests());
		}
	}
}
