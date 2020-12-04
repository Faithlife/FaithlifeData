using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using MySqlConnector;
using NUnit.Framework;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class MySqlTests
	{
		[Test, Explicit("Requires 'docker-compose up' from '/docker'.")]
		public void PrepareCacheTests()
		{
			var tableName = Sql.Raw(nameof(PrepareCacheTests));

			using var connector = CreateConnector();
			connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
			connector.Command(Sql.Format($"create table {tableName} (Id int not null auto_increment primary key, Name varchar(100) not null);")).Execute();

			var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
			connector.Command(insertSql, ("itemA", "one"), ("itemB", "two")).Prepare().Cache().Execute().Should().Be(2);
			connector.Command(insertSql, ("itemA", "three"), ("itemB", "four")).Prepare().Cache().Execute().Should().Be(2);
			connector.Command(insertSql, ("itemB", "six"), ("itemA", "five")).Prepare().Cache().Execute().Should().Be(2);

			connector.Command(Sql.Format($"select Name from {tableName} order by Id;")).Query<string>().Should().Equal("one", "two", "three", "four", "five", "six");
		}

		private static DbConnector CreateConnector() => DbConnector.Create(
			new MySqlConnection("Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=mysqlconnector;Ignore Prepare=false"),
			new DbConnectorSettings { AutoOpen = true, LazyOpen = true });
	}
}
