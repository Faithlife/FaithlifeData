using System.Data;
using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace Faithlife.Data.Tests;

[TestFixture, Explicit("Requires 'docker-compose up' from '/docker'.")]
public class SqlServerTests
{
	[Test]
	public void PrepareCacheTests()
	{
		var tableName = Sql.Name(nameof(PrepareCacheTests));

		using var connector = CreateConnector();
		connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
		connector.Command(Sql.Format($"create table {tableName} (ItemId int not null identity primary key, Name nvarchar(100) not null);")).Execute();

		var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
		connector.Command(insertSql, ("itemA", CreateStringParameter("one")), ("itemB", CreateStringParameter("two"))).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql, ("itemA", CreateStringParameter("three")), ("itemB", CreateStringParameter("four"))).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql, ("itemB", CreateStringParameter("six")), ("itemA", CreateStringParameter("five"))).Prepare().Cache().Execute().Should().Be(2);

		// fails if parameters aren't reused properly
		connector.Command(Sql.Format($"select Name from {tableName} order by ItemId;"))
			.Query<string>().Should().Equal("one", "two", "three", "four", "five", "six");

		// SqlCommand.Prepare method requires all parameters to have an explicitly set type
		SqlParameter CreateStringParameter(string value) => new SqlParameter { Value = value, DbType = DbType.String, Size = 100 };
	}

	private static DbConnector CreateConnector() => DbConnector.Create(
		new SqlConnection("data source=localhost;user id=sa;password=P@ssw0rd;initial catalog=test;TrustServerCertificate=True"),
		new DbConnectorSettings { AutoOpen = true, LazyOpen = true, SqlSyntax = SqlSyntax.SqlServer });
}
