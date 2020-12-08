using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class SqliteTests
	{
		[Test]
		public void PrepareCacheTests()
		{
			var tableName = Sql.Name(nameof(PrepareCacheTests));

			using var connector = CreateConnector();
			connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
			connector.Command(Sql.Format($"create table {tableName} (ItemId integer primary key, Name text not null);")).Execute();

			var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
			connector.Command(insertSql, ("itemA", "one"), ("itemB", "two")).Prepare().Cache().Execute().Should().Be(2);
			connector.Command(insertSql, ("itemA", "three"), ("itemB", "four")).Prepare().Cache().Execute().Should().Be(2);
			connector.Command(insertSql, ("itemB", "six"), ("itemA", "five")).Prepare().Cache().Execute().Should().Be(2);

			// fails if parameters aren't reused properly
			connector.Command(Sql.Format($"select Name from {tableName} order by ItemId;"))
				.Query<string>().Should().Equal("one", "two", "three", "four", "five", "six");
		}

		private static DbConnector CreateConnector() => DbConnector.Create(
			new SqliteConnection("Data Source=:memory:"),
			new DbConnectorSettings { AutoOpen = true, LazyOpen = true, SqlSyntax = SqlSyntax.Sqlite });
	}
}
