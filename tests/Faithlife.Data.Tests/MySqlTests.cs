using System.Data;
using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using MySqlConnector;
using NUnit.Framework;

namespace Faithlife.Data.Tests
{
	[TestFixture, Explicit("Requires 'docker-compose up' from '/docker'.")]
	public class MySqlTests
	{
		[Test]
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

		[Test]
		public void SprocInOutTest()
		{
			var sprocName = nameof(SprocInOutTest);

			using var connector = CreateConnector();
			connector.Command(Sql.Format($"drop procedure if exists {sprocName:raw};")).Execute();
			connector.Command(Sql.Format($"create procedure {sprocName:raw} (inout Value int) begin set Value = Value * Value; end;")).Execute();

			var param = new MySqlParameter { DbType = DbType.Int32, Direction = ParameterDirection.InputOutput, Value = 11 };
			connector.StoredProcedure(sprocName, ("Value", param)).Execute();
			param.Value.Should().Be(121);
		}

		[Test]
		public void SprocInTest()
		{
			var sprocName = nameof(SprocInTest);

			using var connector = CreateConnector();
			connector.Command(Sql.Format($"drop procedure if exists {sprocName:raw};")).Execute();
			connector.Command(Sql.Format($"create procedure {sprocName:raw} (in Value int) begin select Value, Value * Value; end;")).Execute();

			connector.StoredProcedure(sprocName, ("Value", 11)).QuerySingle<(int, long)>().Should().Be((11, 121));
		}

		private static DbConnector CreateConnector() => DbConnector.Create(
			new MySqlConnection("Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=mysqlconnector;Ignore Prepare=false"),
			new DbConnectorSettings { AutoOpen = true, LazyOpen = true });
	}
}
