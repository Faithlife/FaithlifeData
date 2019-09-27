using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
				connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
				connector.Command("insert into Items (Name) values ('item1'); insert into Items (Name) values ('item2');").Execute().Should().Be(2);
				connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("item1", "item2");
				connector.Command("select Name from Items order by ItemId;").Query(ToUpper).Should().Equal("ITEM1", "ITEM2");
				connector.Command("select Name from Items order by ItemId;").Enumerate<string>().Should().Equal("item1", "item2");
				connector.Command("select Name from Items order by ItemId;").Enumerate(ToUpper).Should().Equal("ITEM1", "ITEM2");
				connector.Command("select Name from Items order by ItemId;").QueryFirst<string>().Should().Be("item1");
				connector.Command("select Name from Items order by ItemId;").QueryFirst(ToUpper).Should().Be("ITEM1");
				connector.Command("select Name from Items order by ItemId;").QueryFirstOrDefault<string>().Should().Be("item1");
				connector.Command("select Name from Items order by ItemId;").QueryFirstOrDefault(ToUpper).Should().Be("ITEM1");
				connector.Command("select Name from Items order by ItemId limit 1;").QuerySingle<string>().Should().Be("item1");
				connector.Command("select Name from Items order by ItemId limit 1;").QuerySingle(ToUpper).Should().Be("ITEM1");
				connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefault<string>().Should().Be("item1");
				connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefault(ToUpper).Should().Be("ITEM1");
				Invoking(() => connector.Command("select Name from Items where Name = 'nope';").QueryFirst<string>()).Should().Throw<InvalidOperationException>();
				Invoking(() => connector.Command("select Name from Items;").QuerySingle<string>()).Should().Throw<InvalidOperationException>();
				connector.Command("select Name from Items where Name = 'nope';").QueryFirstOrDefault<string>().Should().BeNull();
				connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QueryFirstOrDefault<long>().Should().NotBe(0);
				connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QueryFirst<long>().Should().NotBe(0);
				connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QuerySingleOrDefault<long>().Should().NotBe(0);
				connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QuerySingle<long>().Should().NotBe(0);
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
				(await connector.Command("select Name from Items order by ItemId;").QueryAsync<string>()).Should().Equal("item1", "item2");
				(await connector.Command("select Name from Items order by ItemId;").QueryAsync(ToUpper)).Should().Equal("ITEM1", "ITEM2");
				(await connector.Command("select Name from Items order by ItemId;").QueryFirstAsync<string>()).Should().Be("item1");
				(await connector.Command("select Name from Items order by ItemId;").QueryFirstAsync(ToUpper)).Should().Be("ITEM1");
				(await connector.Command("select Name from Items order by ItemId;").QueryFirstOrDefaultAsync<string>()).Should().Be("item1");
				(await connector.Command("select Name from Items order by ItemId;").QueryFirstOrDefaultAsync(ToUpper)).Should().Be("ITEM1");
				(await connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleAsync<string>()).Should().Be("item1");
				(await connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleAsync(ToUpper)).Should().Be("ITEM1");
				(await connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefaultAsync<string>()).Should().Be("item1");
				(await connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefaultAsync(ToUpper)).Should().Be("ITEM1");
				await Invoking(async () => await connector.Command("select Name from Items where Name = 'nope';").QueryFirstAsync<string>()).Should().ThrowAsync<InvalidOperationException>();
				await Invoking(async () => await connector.Command("select Name from Items;").QuerySingleAsync<string>()).Should().ThrowAsync<InvalidOperationException>();
				(await connector.Command("select Name from Items where Name = 'nope';").QueryFirstOrDefaultAsync<string>()).Should().BeNull();
				(await connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QueryFirstOrDefaultAsync<long>()).Should().NotBe(0);
				(await connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QueryFirstAsync<long>()).Should().NotBe(0);
				(await connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QuerySingleOrDefaultAsync<long>()).Should().NotBe(0);
				(await connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QuerySingleAsync<long>()).Should().NotBe(0);
			}
		}

		[Test]
		public void ParametersTests()
		{
			using (var connector = CreateConnector())
			using (connector.OpenConnection())
			{
				connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
				connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);",
					("item1", "one"), ("item2", "two")).Execute().Should().Be(2);
				connector.Command("select Name from Items where Name like @like;",
					DbParameters.Empty.Add("like", "t%")).QueryFirst<string>().Should().Be("two");
			}
		}

		[Test]
		public async Task ParametersAsyncTests()
		{
			using (var connector = CreateConnector())
			using (await connector.OpenConnectionAsync())
			{
				(await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
				(await connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);",
					("item1", "one"), ("item2", "two")).ExecuteAsync()).Should().Be(2);
				(await connector.Command("select Name from Items where Name like @like;",
					DbParameters.Empty.Add("like", "t%")).QueryFirstAsync<string>()).Should().Be("two");
			}
		}

		[Test]
		public void ParametersFromDtoTests()
		{
			using (var connector = CreateConnector())
			using (connector.OpenConnection())
			{
				const string item1 = "one";
				const string item2 = "two";
				connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
				connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);",
					DbParameters.FromDto(new { item1, item2 })).Execute().Should().Be(2);
				connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal(item1, item2);
			}
		}

		[Test]
		public void TransactionTests([Values] bool? commit)
		{
			using (var connector = CreateConnector())
			using (connector.OpenConnection())
			{
				connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

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
				await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();

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
				connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

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

		[Test]
		public void QueryMultipleTests()
		{
			using (var connector = CreateConnector())
			using (connector.OpenConnection())
			{
				connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
				connector.Command("insert into Items (Name) values ('item1'), ('item2');").Execute();

				using (var resultSet = connector.Command(@"
					select ItemId from Items where Name = 'item1';
					select ItemId from Items where Name = 'item2';
					").QueryMultiple())
				{
					long id1 = resultSet.Read<long>().Single();
					long id2 = resultSet.Read<long>().Single();
					id1.Should().BeLessThan(id2);
					Invoking(() => resultSet.Read(x => 0)).Should().Throw<InvalidOperationException>();
				}
			}
		}

		[Test]
		public async Task BadCommandTest()
		{
			Invoking(() => default(DbConnectorCommand).Create()).Should().Throw<InvalidOperationException>();
		}

		private DbConnector CreateConnector() => DbConnector.Create(
			new SQLiteConnection("Data Source=:memory:"),
			new DbConnectorSettings { ProviderMethods = new SqliteProviderMethods() });

		private string ToUpper(IDataRecord x) => x.Get<string>().ToUpperInvariant();
	}
}
