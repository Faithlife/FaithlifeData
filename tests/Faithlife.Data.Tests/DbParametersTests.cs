using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests;

[TestFixture]
internal sealed class DbParametersTests
{
	[Test]
	public void Empty()
	{
		default(DbParameters).Should().BeEmpty();
		DbParameters.Empty.Should().BeEmpty();
	}

	[Test]
	public void CreateSingle()
	{
		DbParameters.Create("one", 1).Should().Equal(("one", 1));
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
	public void CreateManyWithOneName()
	{
		DbParameters.FromMany("one", new object?[] { 1, "two", null }).Should().Equal(("one_0", 1), ("one_1", "two"), ("one_2", null));
		DbParameters.FromMany((int i) => $"fancy*{2 * i + 1}", new object?[] { 3.14, false }).Should().Equal(("fancy*1", 3.14), ("fancy*3", false));
	}

	[Test]
	public void CreateFromDto()
	{
		DbParameters.FromDto(new { one = 1 }).AddDto(new HasTwo()).Should().Equal(("one", 1), ("Two", 2));
		DbParameters.FromDto("Thing", new { one = 1, Two = 2 }).Should().Equal(("Thing_one", 1), ("Thing_Two", 2));
		DbParameters.FromDto((string prop) => $"it's {prop}", new { one = 1, Two = 2 }).Should().Equal(("it's one", 1), ("it's Two", 2));
	}

	[Test]
	public void CreateFromDtos()
	{
		DbParameters.FromDtos(new object[] { new { zero = 0, one = 1 }, new HasTwo() }).Should().Equal(("zero_0", 0), ("one_0", 1), ("Two_1", 2));
		DbParameters.FromDtos("very", new object[] { new { zero = 0, one = 1 }, new HasTwo() }).Should().Equal(("very_zero_0", 0), ("very_one_0", 1), ("very_Two_1", 2));
		DbParameters.FromDtos((string prop, int i) => $"{prop}? more like {(i + 1) * 100}", new object[] { new { zero = 0, one = 1 }, new HasTwo() }).Should().Equal(("zero? more like 100", 0), ("one? more like 100", 1), ("Two? more like 200", 2));
	}

	[Test]
	public void CreateFromDtoWhere()
	{
		DbParameters.FromDtoWhere(new { one = 1, two = 2, three = 3 }, x => x[0] == 't').Should().Equal(("two", 2), ("three", 3));
		DbParameters.FromDtoWhere("thing", new { one = 1, two = 2, three = 3 }, x => x[0] == 't').Should().Equal(("thing_two", 2), ("thing_three", 3));
		DbParameters.FromDtoWhere(x => x.ToUpperInvariant(), new { one = 1, two = 2, three = 3 }, x => x[0] == 't').Should().Equal(("TWO", 2), ("THREE", 3));
	}

	[Test]
	public void Add()
	{
		default(DbParameters)
			.Add("one", 1)
			.Add(("two", 2L))
			.Add()
			.Add(("three", 3.0f), ("four", 4.0))
			.Add(new[] { ("five", 5) })
			.Add(new Dictionary<string, int> { { "six", 6 } })
			.AddMany("seven", new object?[] { 7, "8", null })
			.AddMany((int i) => $"ten*{2 * i + 1}", new object?[] { 10.0, false })
			.AddDto(new { twelve = 12 })
			.AddDto("the", new { thirteen = 13 })
			.AddDto(name => $"Why @{name}?", new { fourteen = 14 })
			.AddDtos(new object[] { new { fifteen = 15, sixteen = 16 }, new { seventeen = 17 } })
			.AddDtos("stop", new object[] { new { eighteen = 18, nineteen = 19 }, new { twenty = 20 } })
			.AddDtos((string prop, int i) => $"I don't want to write any more {prop}ing numbers ({i + 1})", new object[] { new { twenty_one = 21, twenty_two = 22 }, new { twenty_three = 23 } })
			.Should()
			.HaveCount(23);
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
		Invoking(() => default(DbParameters).Add(null!)).Should().Throw<ArgumentNullException>();
		Invoking(() => DbParameters.Create(default((string, string)[])!)).Should().Throw<ArgumentNullException>();
		Invoking(() => default(DbParameters).Add(default((string, string)[])!)).Should().Throw<ArgumentNullException>();
		Invoking(() => DbParameters.Create(default(Dictionary<string, string>)!)).Should().Throw<ArgumentNullException>();
		Invoking(() => default(DbParameters).Add(default(Dictionary<string, string>)!)).Should().Throw<ArgumentNullException>();
		Invoking(() => DbParameters.FromDto(null!)).Should().Throw<ArgumentNullException>();
		Invoking(() => default(DbParameters).AddDto(null!)).Should().Throw<ArgumentNullException>();
	}

	private sealed class HasTwo
	{
		public int Two { get; } = 2;
	}
}
