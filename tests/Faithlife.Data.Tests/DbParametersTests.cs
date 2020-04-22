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
		public void CreateFromPairParams()
		{
			DbParameters.Create().Should().BeEmpty();
			DbParameters.Create(("one", 1)).Should().Equal(("one", 1));
			DbParameters.Create(("one", 1), ("two", 2L)).Should().Equal(("one", 1), ("two", 2L));
			DbParameters.Create(("one", 1), ("two", "2")).Should().Equal(("one", 1), ("two", "2"));
			DbParameters.Create(("one", 1), ("null", null)).Should().Equal(("one", 1), ("null", null));
		}

		[Test]
		public void CreateFromPairArray()
		{
			DbParameters.Create(new[] { ("one", "1"), ("two", "2") }).Should().Equal(("one", "1"), ("two", "2"));
			DbParameters.Create(new[] { ("one", 1), ("two", 2L) }).Should().Equal(("one", 1L), ("two", 2L));
			var array1 = new (string, object)[] { ("one", 1), ("two", 2L) };
			DbParameters.Create(array1!).Should().Equal(("one", 1), ("two", 2L));
			var array2 = new (string, object?)[] { ("one", 1), ("two", 2L) };
			DbParameters.Create(array2).Should().Equal(("one", 1), ("two", 2L));
			var array3 = new[] { ("one", (object) 1), ("two", 2L) };
			DbParameters.Create(array3!).Should().Equal(("one", 1), ("two", 2L));
			var array4 = new[] { ("one", (object?) 1), ("two", 2L) };
			DbParameters.Create(array4).Should().Equal(("one", 1), ("two", 2L));
		}

		[Test]
		public void CreateFromPairList()
		{
			DbParameters.Create(new List<(string, long)> { ("one", 1), ("two", 2L) }).Should().Equal(("one", 1L), ("two", 2L));
			DbParameters.Create(new List<(string, int?)> { ("one", 1), ("null", null) }).Should().Equal(("one", 1), ("null", null));
			DbParameters.Create(new List<(string, object)> { ("one", 1), ("two", 2L) }!).Should().Equal(("one", 1), ("two", 2L));
			DbParameters.Create(new List<(string, object?)> { ("one", 1), ("null", null) }).Should().Equal(("one", 1), ("null", null));
		}

		[Test]
		public void CreateFromDictionary()
		{
			DbParameters.Create(new Dictionary<string, long> { { "one", 1 }, { "two", 2L } }).Should().Equal(("one", 1L), ("two", 2L));
			DbParameters.Create(new Dictionary<string, int?> { { "one", 1 }, { "null", null } }).Should().Equal(("one", 1), ("null", null));
			DbParameters.Create(new Dictionary<string, object> { { "one", 1 }, { "two", 2L } }).Should().Equal(("one", 1), ("two", 2L));
			DbParameters.Create(new Dictionary<string, object?> { { "one", 1 }, { "null", null } }).Should().Equal(("one", 1), ("null", null));
		}

		[Test]
		public void CreateFromDto()
		{
			DbParameters.FromDto(new { one = 1 }).AddDto(new HasTwo()).Should().Equal(("one", 1), ("Two", 2));
		}

		[Test]
		public void Add()
		{
			new DbParameters()
				.Add("one", 1)
				.Add(("two", 2L))
				.Add()
				.Add(("three", 3.0f), ("four", 4.0))
				.Add(new[] { ("five", 5) })
				.Add(new Dictionary<string, int> { { "six", 6 } })
				.Should()
				.HaveCount(6);
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
			Invoking(() => DbParameters.Create(null!)).Should().Throw<ArgumentNullException>();
			Invoking(() => new DbParameters().Add(null!)).Should().Throw<ArgumentNullException>();
			Invoking(() => DbParameters.Create(default((string, string)[])!)).Should().Throw<ArgumentNullException>();
			Invoking(() => new DbParameters().Add(default((string, string)[])!)).Should().Throw<ArgumentNullException>();
			Invoking(() => DbParameters.Create(default(Dictionary<string, string>)!)).Should().Throw<ArgumentNullException>();
			Invoking(() => new DbParameters().Add(default(Dictionary<string, string>)!)).Should().Throw<ArgumentNullException>();
			Invoking(() => DbParameters.FromDto(null!)).Should().Throw<ArgumentNullException>();
			Invoking(() => new DbParameters().AddDto(null!)).Should().Throw<ArgumentNullException>();
		}

		private sealed class HasTwo
		{
			public int Two { get; } = 2;
		}
	}
}
