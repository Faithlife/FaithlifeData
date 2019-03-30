using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using static Faithlife.Data.Tests.FluentAction;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	[SuppressMessage("ReSharper", "ConsiderUsingConfigureAwait")]
	public class DbConnectorTests
	{
		[Test]
		public void NullConnection()
		{
			Invoking(() => DbConnector.Create(null)).Should().Throw<ArgumentNullException>();
		}

		[Test]
		public void CommandTests()
		{
			using (var connector = CreateConnector())
			using (connector.OpenConnection())
			{
				connector.Command("create table Items (ItemId bigint primary key, Name text not null);").Execute().Should().Be(0);
				connector.Command("insert into Items (Name) values ('item1'); insert into Items (Name) values ('item2');").Execute().Should().Be(2);
				connector.Command("select Name from Items;").Query<string>().Should().Equal("item1", "item2");
				connector.Command("select Name from Items;").Query(ToUpper).Should().Equal("ITEM1", "ITEM2");
				connector.Command("select Name from Items;").Enumerate<string>().Should().Equal("item1", "item2");
				connector.Command("select Name from Items;").Enumerate(ToUpper).Should().Equal("ITEM1", "ITEM2");
				connector.Command("select Name from Items;").QueryFirst<string>().Should().Be("item1");
				connector.Command("select Name from Items;").QueryFirst(ToUpper).Should().Be("ITEM1");
				connector.Command("select Name from Items;").QueryFirstOrDefault<string>().Should().Be("item1");
				connector.Command("select Name from Items;").QueryFirstOrDefault(ToUpper).Should().Be("ITEM1");
				Invoking(() => connector.Command("select Name from Items where Name = 'nope';").QueryFirst<string>()).Should().Throw<InvalidOperationException>();
				connector.Command("select Name from Items where Name = 'nope';").QueryFirstOrDefault<string>().Should().BeNull();
			}
		}

		[Test]
		public async Task CommandAsyncTests()
		{
			using (var connector = CreateConnector())
			using (await connector.OpenConnectionAsync())
			{
				(await connector.Command("create table Items (ItemId bigint primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
				(await connector.Command("insert into Items (Name) values ('item1'); insert into Items (Name) values ('item2');").ExecuteAsync()).Should().Be(2);
				(await connector.Command("select Name from Items;").QueryAsync<string>()).Should().Equal("item1", "item2");
				(await connector.Command("select Name from Items;").QueryAsync(ToUpper)).Should().Equal("ITEM1", "ITEM2");
				(await connector.Command("select Name from Items;").QueryFirstAsync<string>()).Should().Be("item1");
				(await connector.Command("select Name from Items;").QueryFirstAsync(ToUpper)).Should().Be("ITEM1");
				(await connector.Command("select Name from Items;").QueryFirstOrDefaultAsync<string>()).Should().Be("item1");
				(await connector.Command("select Name from Items;").QueryFirstOrDefaultAsync(ToUpper)).Should().Be("ITEM1");
				await Invoking(async () => await connector.Command("select Name from Items where Name = 'nope';").QueryFirstAsync<string>()).Should().ThrowAsync<InvalidOperationException>();
				(await connector.Command("select Name from Items where Name = 'nope';").QueryFirstOrDefaultAsync<string>()).Should().BeNull();
			}
		}

		[Test]
		public void ParametersTests()
		{
			using (var connector = CreateConnector())
			using (connector.OpenConnection())
			{
				connector.Command("create table Items (ItemId bigint primary key, Name text not null);").Execute().Should().Be(0);
				connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);",
					("item1", "one"), ("item2", "two")).Execute().Should().Be(2);
				connector.Command("select Name from Items where Name like @like;",
					new DbParameters().Add("like", "t%")).QueryFirst<string>().Should().Be("two");
			}
		}

		[Test]
		public void TransactionTests([Values] bool? commit)
		{
			using (var connector = CreateConnector())
			using (connector.OpenConnection())
			{
				connector.Command("create table Items (ItemId bigint primary key, Name text not null);").Execute();

				using (connector.BeginTransaction())
				{
					connector.Command("insert into Items (Name) values ('item1');").Execute();
					if (commit == true)
						connector.CommitTransaction();
					else if (commit == false)
						connector.RollbackTransaction();
				}

				connector.Command("select count(*) from Items;").QueryFirst<long>().Should().Be(commit == true ? 1 : 0);
			}
		}

		[Test]
		public async Task TransactionAsyncTests([Values] bool? commit)
		{
			using (var connector = CreateConnector())
			using (await connector.OpenConnectionAsync())
			{
				await connector.Command("create table Items (ItemId bigint primary key, Name text not null);").ExecuteAsync();

				using (await connector.BeginTransactionAsync())
				{
					await connector.Command("insert into Items (Name) values ('item1');").ExecuteAsync();
					if (commit == true)
						await connector.CommitTransactionAsync();
					else if (commit == false)
						await connector.RollbackTransactionAsync();
				}

				(await connector.Command("select count(*) from Items;").QueryFirstAsync<long>()).Should().Be(commit == true ? 1 : 0);
			}
		}

		[Test]
		public async Task IsolationLevelTests()
		{
			using (var connector = CreateConnector())
			using (connector.OpenConnection())
			{
				connector.Command("create table Items (ItemId bigint primary key, Name text not null);").Execute();

				using (connector.BeginTransaction(IsolationLevel.ReadCommitted))
				{
					connector.Command("insert into Items (Name) values ('item1');").Execute();
					connector.CommitTransaction();
				}

				using (await connector.BeginTransactionAsync(IsolationLevel.ReadCommitted))
				{
					await connector.Command("insert into Items (Name) values ('item2');").ExecuteAsync();
					await connector.CommitTransactionAsync();
				}

				connector.Command("select count(*) from Items;").QueryFirst<long>().Should().Be(2);
			}
		}

		private DbConnector CreateConnector() => DbConnector.Create(
			new SQLiteConnection("Data Source=:memory:"),
			new DbConnectorSettings { ProviderMethods = new SqliteProviderMethods() });

		private string ToUpper(IDataRecord x) => x.Get<string>().ToUpperInvariant();
	}
}
