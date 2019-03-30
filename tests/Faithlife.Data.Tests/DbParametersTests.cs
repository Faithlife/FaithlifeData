using System;
using FluentAssertions;
using NUnit.Framework;
using static Faithlife.Data.Tests.FluentAction;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class DbParametersTests
	{
		[Test]
		public void DefaultConstructor()
		{
			new DbParameters().Should().BeEmpty();
		}

		[Test]
		public void ConstructFromPairs()
		{
			var parameters = new DbParameters(new (string, object)[] { ("one", 1), ("two", 2L) });
			parameters.Count.Should().Be(2);
			parameters[0].Should().Be(("one", 1));
			parameters[1].Should().Be(("two", 2L));
		}

		[Test]
		public void CreateFromPairs()
		{
			DbParameters.Create().Should().BeEmpty();
			DbParameters.Create(("one", 1)).Should().Equal(("one", 1));
			DbParameters.Create(("one", 1), ("two", 2L)).Should().Equal(("one", 1), ("two", 2L));
		}

		[Test]
		public void Add()
		{
			new DbParameters().Add("one", 1).Add(("two", 2L)).Add().Add(("three", 3.0f), ("four", 4.0)).Should().HaveCount(4);
		}

		[Test]
		public void InitializationSyntax()
		{
			new DbParameters { ("one", 1), ("two", 2L) }.Should().HaveCount(2);
			new DbParameters { { "one", 1 }, { "two", 2L } }.Should().HaveCount(2);
		}

		[Test]
		public void Nulls()
		{
			Invoking(() => new DbParameters(null)).Should().Throw<ArgumentNullException>();
			Invoking(() => DbParameters.Create(null)).Should().Throw<ArgumentNullException>();
			Invoking(() => new DbParameters().Add(null)).Should().Throw<ArgumentNullException>();
		}
	}
}
