using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class NpgsqlTests
	{
		[Test, Explicit("Requires 'docker-compose up' from '/docker'.")]
		public void PrepareCacheTests()
		{
			using var connector = CreateConnector();
			connector.Command("drop table if exists Items;").Execute();
			connector.Command("create table Items (ItemId serial primary key, Name varchar not null);").Execute();

			var insertSql = "insert into Items (Name) values (@itemA); insert into Items (Name) values (@itemB);";
			connector.Command(insertSql, ("itemA", "one"), ("itemB", "two")).Prepare().Cache().Execute().Should().Be(2);
			connector.Command(insertSql, ("itemA", "three"), ("itemB", "four")).Prepare().Cache().Execute().Should().Be(2);
			connector.Command(insertSql, ("itemB", "six"), ("itemA", "five")).Prepare().Cache().Execute().Should().Be(2);

			// fails if parameters aren't reused properly
			connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two", "three", "four", "five", "six");
		}

		private static DbConnector CreateConnector() => DbConnector.Create(
			new NpgsqlConnection("host=localhost;user id=root;password=test;database=test"),
			new DbConnectorSettings { AutoOpen = true, LazyOpen = true });
	}
}
