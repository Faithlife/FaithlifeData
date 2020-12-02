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
			using var connector = CreateConnector();
			connector.Command("drop table if exists Items;").Execute();
			connector.Command("create table Items (ItemId int not null auto_increment primary key, Name varchar(100) not null);").Execute();

			var insertSql = "insert into Items (Name) values (@itemA); insert into Items (Name) values (@itemB);";
			connector.Command(insertSql, ("itemA", "one"), ("itemB", "two")).Prepare().Cache().Execute().Should().Be(2);
			connector.Command(insertSql, ("itemA", "three"), ("itemB", "four")).Prepare().Cache().Execute().Should().Be(2);
			connector.Command(insertSql, ("itemB", "six"), ("itemA", "five")).Prepare().Cache().Execute().Should().Be(2);

			connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two", "three", "four", "five", "six");
		}

		private static DbConnector CreateConnector() => DbConnector.Create(
			new MySqlConnection("Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=mysqlconnector;Ignore Prepare=false"),
			new DbConnectorSettings { AutoOpen = true, LazyOpen = true });
	}
}
