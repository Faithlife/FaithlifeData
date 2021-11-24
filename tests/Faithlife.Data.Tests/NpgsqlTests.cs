using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace Faithlife.Data.Tests;

[TestFixture, Explicit("Requires 'docker-compose up' from '/docker'.")]
public class NpgsqlTests
{
	[Test]
	public void PrepareCacheTests()
	{
		var tableName = Sql.Name(nameof(PrepareCacheTests));

		using var connector = CreateConnector();
		connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
		connector.Command(Sql.Format($"create table {tableName} (ItemId serial primary key, Name varchar not null);")).Execute();

		var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
		connector.Command(insertSql, ("itemA", "one"), ("itemB", "two")).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql, ("itemA", "three"), ("itemB", "four")).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql, ("itemB", "six"), ("itemA", "five")).Prepare().Cache().Execute().Should().Be(2);

		// fails if parameters aren't reused properly
		connector.Command(Sql.Format($"select Name from {tableName} order by ItemId;"))
			.Query<string>().Should().Equal("one", "two", "three", "four", "five", "six");
	}

	private static DbConnector CreateConnector() => DbConnector.Create(
		new NpgsqlConnection("host=localhost;user id=root;password=test;database=test"),
		new DbConnectorSettings { AutoOpen = true, LazyOpen = true, SqlSyntax = SqlSyntax.Postgres });
}
