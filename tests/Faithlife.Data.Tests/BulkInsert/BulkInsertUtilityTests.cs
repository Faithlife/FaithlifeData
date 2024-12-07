using Faithlife.Data.BulkInsert;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests.BulkInsert;

[TestFixture]
internal sealed class BulkInsertUtilityTests
{
	[Test]
	public void EmptySql_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("", DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void NoValues_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUE (@foo)...", DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void ValuesSuffix_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("1VALUES (@foo)...", DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void NoEllipsis_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUE (@foo)..", DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void MultipleValues_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUES (@foo)... VALUES (@foo)...", DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void ZeroBatchSize_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUES (@foo)...",
				DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }, new BulkInsertSettings { MaxRowsPerBatch = 0 }).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void NegativeBatchSize_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUES (@foo)...",
				DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }, new BulkInsertSettings { MaxRowsPerBatch = -1 }).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void MinimalInsert()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("INSERT INTO t (foo)VALUES(@foo)...;",
			DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("INSERT INTO t (foo)VALUES(@foo_0);");
		commands[0].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "foo_0", 1 } });
	}

	[Test]
	public void InsertNotRequired()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@foo)...",
			DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@foo_0)");
		commands[0].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "foo_0", 1 } });
	}

	[Test]
	public void MultipleInserts()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("INSERT INTO t VALUES (@t); INSERT INTO u VALUES (@u)...; INSERT INTO v VALUES (@v);",
			DbParameters.Empty, new[] { DbParameters.FromDto(new { t = 1, u = 2, v = 3 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("INSERT INTO t VALUES (@t); INSERT INTO u VALUES (@u_0); INSERT INTO v VALUES (@v);");
		commands[0].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "u_0", 2 } });
	}

	[Test]
	public void CommonAndInsertedParameters()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@a, @b, @c, @d)...",
			DbParameters.FromDto(new { a = 1, b = 2 }), new[] { DbParameters.FromDto(new { c = 3, d = 4 }), DbParameters.FromDto(new { c = 5, d = 6 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@a, @b, @c_0, @d_0), (@a, @b, @c_1, @d_1)");
		commands[0].Parameters.ToDictionary().Should().Equal(
			new Dictionary<string, object?>
			{
				{ "a", 1 },
				{ "b", 2 },
				{ "c_0", 3 },
				{ "d_0", 4 },
				{ "c_1", 5 },
				{ "d_1", 6 },
			});
	}

	[TestCase(3, null)]
	[TestCase(null, 6)]
	[TestCase(3, 10)]
	[TestCase(10, 6)]
	public void EightRowsInThreeBatches(int? maxRecordsPerBatch, int? maxParametersPerBatch)
	{
		var settings = new BulkInsertSettings
		{
			MaxRowsPerBatch = maxRecordsPerBatch,
			MaxParametersPerBatch = maxParametersPerBatch,
		};
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES(@foo,@bar)...",
			DbParameters.Empty, Enumerable.Range(0, 8).Select(x => DbParameters.FromDto(new { foo = x, bar = x * 2 })), settings).ToList();
		commands.Count.Should().Be(3);
		commands[0].Sql.Should().Be("VALUES(@foo_0,@bar_0), (@foo_1,@bar_1), (@foo_2,@bar_2)");
		commands[0].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "foo_0", 0 }, { "bar_0", 0 }, { "foo_1", 1 }, { "bar_1", 2 }, { "foo_2", 2 }, { "bar_2", 4 } });
		commands[1].Sql.Should().Be("VALUES(@foo_0,@bar_0), (@foo_1,@bar_1), (@foo_2,@bar_2)");
		commands[1].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "foo_0", 3 }, { "bar_0", 6 }, { "foo_1", 4 }, { "bar_1", 8 }, { "foo_2", 5 }, { "bar_2", 10 } });
		commands[2].Sql.Should().Be("VALUES(@foo_0,@bar_0), (@foo_1,@bar_1)");
		commands[2].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "foo_0", 6 }, { "bar_0", 12 }, { "foo_1", 7 }, { "bar_1", 14 } });
	}

	[Test]
	public void CaseInsensitiveValues()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VaLueS(@foo)...",
			DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VaLueS(@foo_0)");
		commands[0].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "foo_0", 1 } });
	}

	[Test]
	public void CaseInsensitiveNames()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("values (@foo, @Bar, @BAZ, @bam)...",
			DbParameters.Empty, new[] { DbParameters.FromDto(new { Foo = 1, BAR = 2, baz = 3 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("values (@foo_0, @Bar_0, @BAZ_0, @bam)");
		commands[0].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "Foo_0", 1 }, { "BAR_0", 2 }, { "baz_0", 3 } });
	}

	[Test]
	public void PunctuatedNames()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("values (@foo, @bar)...",
			DbParameters.Empty, new[] { DbParameters.Create(("@foo", 1), ("@Bar", 2)) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("values (@foo_0, @bar_0)");
		commands[0].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "@foo_0", 1 }, { "@Bar_0", 2 } });
	}

	[Test]
	public void SubstringNames()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("values (@a, @aa, @aaa, @aaaa)...",
			DbParameters.FromDto(new { a = 1, aaa = 3 }), new[] { DbParameters.FromDto(new { aa = 2, aaaa = 4 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("values (@a, @aa_0, @aaa, @aaaa_0)");
		commands[0].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "a", 1 }, { "aa_0", 2 }, { "aaa", 3 }, { "aaaa_0", 4 } });
	}

	[Test]
	public void WhitespaceEverywhere()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("\r\n\t VALUES\n\t \r(\t \r\n@foo \r\n\t)\r\n\t ...\t\r\n",
			DbParameters.Empty, new[] { DbParameters.FromDto(new { foo = 1 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("\r\n\t VALUES\n\t \r(\t \r\n@foo_0 \r\n\t)\t\r\n");
		commands[0].Parameters.ToDictionary().Should().Equal(new Dictionary<string, object?> { { "foo_0", 1 } });
	}

	[Test]
	public void NothingToInsert()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES(@foo)...", DbParameters.Empty, []).ToList();
		commands.Count.Should().Be(0);
	}

	[Test]
	public void NoParameterNameValidation()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@a, @b, @c, @d)...",
			DbParameters.FromDto(new { e = 1, f = 2 }), new[] { DbParameters.FromDto(new { g = 3, h = 4 }), DbParameters.FromDto(new { g = 5, h = 6 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@a, @b, @c, @d), (@a, @b, @c, @d)");
		commands[0].Parameters.ToDictionary().Should().Equal(
			new Dictionary<string, object?>
			{
				{ "e", 1 },
				{ "f", 2 },
			});
	}

	[Test]
	public void ComplexValues()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@a + (@d * @c) -\r\n\t@d)...",
			DbParameters.FromDto(new { a = 1, b = 2 }), new[] { DbParameters.FromDto(new { c = 3, d = 4 }), DbParameters.FromDto(new { c = 5, d = 6 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@a + (@d_0 * @c_0) -\r\n\t@d_0), (@a + (@d_1 * @c_1) -\r\n\t@d_1)");
		commands[0].Parameters.ToDictionary().Should().Equal(
			new Dictionary<string, object?>
			{
				{ "a", 1 },
				{ "b", 2 },
				{ "c_0", 3 },
				{ "d_0", 4 },
				{ "c_1", 5 },
				{ "d_1", 6 },
			});
	}

	[Test]
	public void DifferentParameters()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@a, @b)...",
			DbParameters.FromDto(new { a = 1, b = 2 }),
			new[] { DbParameters.FromDto(new { b = 4 }), DbParameters.Empty, DbParameters.FromDto(new { a = 3 }) }).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@a, @b_0), (@a, @b), (@a_2, @b)");
		commands[0].Parameters.ToDictionary().Should().Equal(
			new Dictionary<string, object?>
			{
				{ "a", 1 },
				{ "b", 2 },
				{ "b_0", 4 },
				{ "a_2", 3 },
			});
	}
}
