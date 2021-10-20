using System.Linq;
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

		[Test]
		public void InsertAndSelectNameValue()
		{
			var tableName = Sql.Name(nameof(InsertAndSelectNameValue));

			using var connector = CreateConnector();
			connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
			connector.Command(Sql.Format($"create table {tableName} (ItemId integer primary key, Name text not null, Value text not null);")).Execute();

			var items = new[] { new NameValue("one", "two"), new NameValue("two", "four") };

			connector.Command(Sql.Format($@"
				insert into {tableName} ({Sql.ColumnNames<NameValue>()})
				values {Sql.Join(", ", items.Select(item => Sql.Format($"({Sql.ColumnParams(item)})")))};
				")).Execute();

			connector.Command(Sql.Format($"select {Sql.ColumnNames<NameValue>()} from {tableName} t order by ItemId;"))
				.Query<NameValue>().Should().Equal(items);
			connector.Command(Sql.Format($"select {Sql.ColumnNames<NameValue>(nameof(InsertAndSelectNameValue))} from {tableName} order by ItemId;"))
				.Query<NameValue>().Should().Equal(items);
			connector.Command(Sql.Format($"select {Sql.ColumnNames<NameValue>("t")} from {tableName} t order by ItemId;"))
				.Query<NameValue>().Should().Equal(items);
		}

		private static DbConnector CreateConnector() => DbConnector.Create(
			new SqliteConnection("Data Source=:memory:"),
			new DbConnectorSettings { AutoOpen = true, LazyOpen = true, SqlSyntax = SqlSyntax.Sqlite });

		private readonly struct NameValue
		{
			public NameValue(string name, string value) => (Name, Value) = (name, value);
			public string Name { get; }
			public string Value { get; }
		}
	}
}
