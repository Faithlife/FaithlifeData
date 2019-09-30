using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class DbParametersTests
	{
		[Test]
		public void Empty()
		{
			new DbParameters().Should().BeEmpty();
			default(DbParameters).Should().BeEmpty();
			DbParameters.Empty.Should().BeEmpty();
		}

		[Test]
		public void CreateFromPairs()
		{
			DbParameters.Create().Should().BeEmpty();
			DbParameters.Create(("one", 1)).Should().Equal(("one", 1));
			DbParameters.Create(("one", 1), ("two", 2L)).Should().Equal(("one", 1), ("two", 2L));
			DbParameters.Create(new List<(string, object)> { ("one", 1), ("two", 2L) }).Should().Equal(("one", 1), ("two", 2L));
		}

		[Test]
		public void CreateFromDto()
		{
			DbParameters.FromDto(new { one = 1 }).AddDto(new HasTwo()).Should().Equal(("one", 1), ("Two", 2));
		}

		[Test]
		public void Add()
		{
			new DbParameters().Add("one", 1).Add(("two", 2L)).Add().Add(("three", 3.0f), ("four", 4.0)).Add(new List<(string, object)> { ("five", 5) }).Should().HaveCount(5);
		}

		[Test]
		public void Count()
		{
			DbParameters.Empty.Count.Should().Be(0);
			DbParameters.Create(("one", 1)).Count.Should().Be(1);
		}

		[Test]
		public void Index()
		{
			DbParameters.Create(("one", 1))[0].Name.Should().Be("one");
		}

		[Test]
		public void Nulls()
		{
			Invoking(() => DbParameters.Create(null)).Should().Throw<ArgumentNullException>();
			Invoking(() => new DbParameters().Add(null)).Should().Throw<ArgumentNullException>();
		}

		private sealed class HasTwo
		{
			public int Two { get; } = 2;
		}
	}
}
