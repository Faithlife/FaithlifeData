using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Faithlife.Data.BulkInsert;
using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	[SuppressMessage("ReSharper", "InterpolatedStringExpressionIsNotIFormattable", Justification = "Custom formatting.")]
	public class DbConnectorTests
	{
		[Test]
		public void NullConnection()
		{
			Invoking(() => DbConnector.Create(null!)).Should().Throw<ArgumentNullException>();
		}

		[Test]
		public void OpenConnection()
		{
			using var connector = DbConnector.Create(new SqliteConnection("Data Source=:memory:"));
			using (connector.OpenConnection())
				connector.Command("create table Items1 (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		}

		[Test]
		public async Task OpenConnectionAsync()
		{
			await using var connector = DbConnector.Create(new SqliteConnection("Data Source=:memory:"));
			await using (await connector.OpenConnectionAsync())
				(await connector.Command("create table Items1 (ItemId integer primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
		}

		[Test]
		public void CommandTests()
		{
			using var connector = CreateConnector();
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

		[Test]
		public async Task CommandAsyncTests()
		{
			await using var connector = CreateConnector();
			(await connector.Command("create table Items (ItemId bigint primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
			(await connector.Command("insert into Items (Name) values ('item1'); insert into Items (Name) values ('item2');").ExecuteAsync()).Should().Be(2);
			(await connector.Command("select Name from Items order by ItemId;").QueryAsync<string>()).Should().Equal("item1", "item2");
			(await connector.Command("select Name from Items order by ItemId;").QueryAsync(ToUpper)).Should().Equal("ITEM1", "ITEM2");
			(await ToListAsync(connector.Command("select Name from Items order by ItemId;").EnumerateAsync<string>())).Should().Equal("item1", "item2");
			(await ToListAsync(connector.Command("select Name from Items order by ItemId;").EnumerateAsync(ToUpper))).Should().Equal("ITEM1", "ITEM2");
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

		[Test]
		public void ParametersTests()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
			connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);",
				("item1", "one"), ("item2", "two")).Execute().Should().Be(2);
			connector.Command("select Name from Items where Name like @like;",
				DbParameters.Create("like", "t%")).QueryFirst<string>().Should().Be("two");
		}

		[Test]
		public async Task ParametersAsyncTests()
		{
			await using var connector = CreateConnector();
			(await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
			(await connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);",
				("item1", "one"), ("item2", "two")).ExecuteAsync()).Should().Be(2);
			(await connector.Command("select Name from Items where Name like @like;",
				DbParameters.Create("like", "t%")).QueryFirstAsync<string>()).Should().Be("two");
		}

		[Test]
		public void ParametersFromDtoTests()
		{
			using var connector = CreateConnector();
			const string item1 = "one";
			const string item2 = "two";
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
			connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);",
				DbParameters.FromDto(new { item1, item2 })).Execute().Should().Be(2);
			connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal(item1, item2);
		}

		[Test]
		public void ParametersFromSqlTests()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
			var item1 = "two";
			var item2 = "t_o";
			connector.Command(Sql.Format(
				$"insert into Items (Name) values ({item1}); insert into Items (Name) values ({item2});")).Execute().Should().Be(2);
			connector.Command(Sql.Format(
				$@"select Name from Items where Name like {Sql.LikePrefixParam("t_")} escape '\';")).QuerySingle<string>().Should().Be("t_o");
		}

		[Test]
		public void PrepareTests()
		{
			using var connector = CreateConnector();
			var createCmd = connector.Command("create table Items (ItemId integer primary key, Name text not null);");
			createCmd.IsPrepared.Should().Be(false);
			createCmd.Execute().Should().Be(0);

			string insertStmt = "insert into Items (Name) values (@item);";
			var preparedCmd = connector.Command(insertStmt, ("item", "one")).Prepare();
			preparedCmd.IsPrepared.Should().Be(true);
			preparedCmd.Execute().Should().Be(1);

			connector.Command(insertStmt, ("item", "two")).Execute().Should().Be(1);
			connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two");
		}

		[Test]
		public void PrepareCacheTests()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

			var insertStmt = "insert into Items (Name) values (@item);";
			connector.Command(insertStmt, ("item", "one")).Prepare().Cache().Execute().Should().Be(1);
			connector.Command(insertStmt, ("item", "two")).Prepare().Cache().Execute().Should().Be(1);

			connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two");
		}

		[Test]
		public async Task PrepareCacheTestsAsync()
		{
			await using var connector = CreateConnector();
			await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();

			var insertStmt = "insert into Items (Name) values (@item);";
			(await connector.Command(insertStmt, ("item", "one")).Prepare().Cache().ExecuteAsync()).Should().Be(1);
			(await connector.Command(insertStmt, ("item", "two")).Prepare().Cache().ExecuteAsync()).Should().Be(1);

			(await connector.Command("select Name from Items order by ItemId;").QueryAsync<string>()).Should().Equal("one", "two");
		}

		[Test]
		public void TransactionTests([Values] bool? commit)
		{
			using var connector = CreateConnector();
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

		[Test]
		public async Task TransactionAsyncTests([Values] bool? commit)
		{
			await using var connector = CreateConnector();
			await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();

			await using (await connector.BeginTransactionAsync())
			{
				await connector.Command("insert into Items (Name) values ('item1');").ExecuteAsync();
				if (commit == true)
					await connector.CommitTransactionAsync();
				else if (commit == false)
					await connector.RollbackTransactionAsync();
			}

			(await connector.Command("select count(*) from Items;").QueryFirstAsync<long>()).Should().Be(commit == true ? 1 : 0);
		}

		[Test]
		public void IsolationLevelTests()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

			using (connector.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				connector.Command("insert into Items (Name) values ('item1');").Execute();
				connector.CommitTransaction();
			}

			connector.Command("select count(*) from Items;").QueryFirst<long>().Should().Be(1);
		}

		[Test]
		public async Task IsolationLevelAsyncTests()
		{
			await using var connector = CreateConnector();
			await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();

			await using (await connector.BeginTransactionAsync(IsolationLevel.ReadCommitted))
			{
				await connector.Command("insert into Items (Name) values ('item2');").ExecuteAsync();
				await connector.CommitTransactionAsync();
			}

			connector.Command("select count(*) from Items;").QueryFirst<long>().Should().Be(1);
		}

		[Test]
		public void CachedWithTransaction()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

			// make sure correct transaction is used in cached command
			foreach (var item in new[] { "one", "two" })
			{
				using (connector.BeginTransaction())
				{
					connector.Command("insert into Items (Name) values (@item);", ("item", item)).Prepare().Cache().Execute().Should().Be(1);
					connector.CommitTransaction();
				}
			}

			connector.Command("insert into Items (Name) values (@item);", ("item", "three")).Prepare().Cache().Execute().Should().Be(1);

			connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two", "three");
		}

		[Test]
		public void DeferredTransaction()
		{
			var connectionString = new SqliteConnectionStringBuilder { DataSource = nameof(DeferredTransaction), Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared }.ConnectionString;
			using var connector1 = DbConnector.Create(new SqliteConnection(connectionString), new DbConnectorSettings { AutoOpen = true });
			using var connector2 = DbConnector.Create(new SqliteConnection(connectionString), new DbConnectorSettings { AutoOpen = true });
			((SqliteConnection) connector1.Connection).DefaultTimeout = 5;
			((SqliteConnection) connector2.Connection).DefaultTimeout = 5;
			connector1.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
			connector1.Command("insert into Items (Name) values ('xyzzy');").Execute();
			using var transaction1 = connector1.AttachTransaction(((SqliteConnection) connector1.Connection).BeginTransaction(deferred: true));
			using var transaction2 = connector2.AttachTransaction(((SqliteConnection) connector2.Connection).BeginTransaction(deferred: true));
			connector1.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(1);
			connector2.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(1);
			connector1.CommitTransaction();
		}

		[Test]
		public void QueryMultipleTests()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
			connector.Command("insert into Items (Name) values ('item1'), ('item2');").Execute();

			const string sql = @"
				select ItemId from Items order by Name;
				select ItemId from Items where Name = 'item2';";

			using (var resultSet = connector.Command(sql).QueryMultiple())
			{
				var id1 = resultSet.Read<long>().First();
				var id2 = resultSet.Read(x => x.Get<long>()).Single();
				id1.Should().BeLessThan(id2);
				Invoking(() => resultSet.Read(x => 0)).Should().Throw<InvalidOperationException>();
			}

			using (var resultSet = connector.Command(sql).QueryMultiple())
			{
				var id1 = resultSet.Enumerate<long>().First();
				var id2 = resultSet.Enumerate(x => x.Get<long>()).Single();
				id1.Should().BeLessThan(id2);
				Invoking(() => resultSet.Enumerate(x => 0).Count()).Should().Throw<InvalidOperationException>();
			}
		}

		[Test]
		public async Task QueryMultipleAsyncTests()
		{
			await using var connector = CreateConnector();
			await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();
			await connector.Command("insert into Items (Name) values ('item1'), ('item2');").ExecuteAsync();

			const string sql = @"
				select ItemId from Items order by Name;
				select ItemId from Items where Name = 'item2';";

			await using (var resultSet = await connector.Command(sql).QueryMultipleAsync())
			{
				var id1 = (await resultSet.ReadAsync<long>()).First();
				var id2 = (await resultSet.ReadAsync(x => x.Get<long>())).Single();
				id1.Should().BeLessThan(id2);
				Awaiting(async () => await resultSet.ReadAsync(x => 0)).Should().Throw<InvalidOperationException>();
			}

			await using (var resultSet = await connector.Command(sql).QueryMultipleAsync())
			{
				var id1 = await FirstAsync(resultSet.EnumerateAsync<long>());
				var id2 = await FirstAsync(resultSet.EnumerateAsync(x => x.Get<long>()));
				id1.Should().BeLessThan(id2);
				Awaiting(async () => await ToListAsync(resultSet.EnumerateAsync(x => 0))).Should().Throw<InvalidOperationException>();
			}
		}

		[Test]
		public void BulkInsertTests()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
			connector.Command("insert into Items (Name) values (@name)...;")
				.BulkInsert(Enumerable.Range(1, 100).Select(x => DbParameters.Create("name", $"item{x}")));
			connector.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(100);
		}

		[Test]
		public async Task BulkInsertAsyncTests()
		{
			await using var connector = CreateConnector();
			await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();
			await connector.Command("insert into Items (Name) values (@name)...;")
				.BulkInsertAsync(Enumerable.Range(1, 100).Select(x => DbParameters.Create("name", $"item{x}")));
			(await connector.Command("select count(*) from Items;").QuerySingleAsync<long>()).Should().Be(100);
		}

		[Test]
		public async Task BadCommandTest()
		{
			Invoking(() => default(DbConnectorCommand).Create()).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void ParameterCollectionTests()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
			connector.Command("insert into Items (Name) values ('one'), ('two'), ('three');").Execute().Should().Be(3);
			var resultSets = connector.Command(@"
				select Name from Items where Name in (@names...);
				select Name from Items where Name not in (@names...);
				select @before + @after;
				", ("before", 1), ("names", new[] { "one", "three", "five" }), ("ignore", new[] { 0 }), ("after", 2)).QueryMultiple();
			resultSets.Read<string>().Should().BeEquivalentTo("one", "three");
			resultSets.Read<string>().Should().BeEquivalentTo("two");
			resultSets.Read<long>().Should().BeEquivalentTo(3);
		}

		[Test]
		public void BadParameterCollectionTests()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
			connector.Command("insert into Items (Name) values ('one'), ('two'), ('three');").Execute().Should().Be(3);
			Invoking(() => connector.Command("select Name from Items where Name in (@names...);", ("names", Array.Empty<string>()))
				.Query<string>()).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void CacheTests()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
			foreach (var name in new[] { "one", "two", "three" })
				connector.Command("insert into Items (Name) values (@name);", ("name", name)).Cache().Execute().Should().Be(1);
			connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two", "three");
		}

		[Test]
		public void CachedParameterErrors()
		{
			using var connector = CreateConnector();
			connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
			var sql = "insert into Items (Name) values (@name);";
			connector.Command(sql, ("name", "one")).Cache().Execute().Should().Be(1);
			Invoking(() => connector.Command(sql, ("name", "two"), ("three", "four")).Cache().Execute()).Should().Throw<InvalidOperationException>();
			Invoking(() => connector.Command(sql, ("title", "three")).Cache().Execute()).Should().Throw<InvalidOperationException>();
			connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one");
		}

		[Test]
		public async Task CacheAsyncTests()
		{
			await using var connector = CreateConnector();
			(await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
			foreach (var name in new[] { "one", "two", "three" })
				(await connector.Command("insert into Items (Name) values (@name);", ("name", name)).Cache().ExecuteAsync()).Should().Be(1);
			(await connector.Command("select Name from Items order by ItemId;").QueryAsync<string>()).Should().Equal("one", "two", "three");
		}

		[Test]
		public void StoredProcedureUnitTests()
		{
			using var connector = CreateConnector();
			var createCommand = connector.Command("create table Items (ItemId integer primary key, Name text not null);");
			createCommand.CommandType.Should().Be(CommandType.Text);
			createCommand.Execute().Should().Be(0);
			connector.Command("insert into Items (Name) values (@item1);", ("item1", "one")).CommandType.Should().Be(CommandType.Text);

			var storedProcedureCommand = connector.StoredProcedure("values (1);");
			storedProcedureCommand.CommandType.Should().Be(CommandType.StoredProcedure);
			Invoking(() => storedProcedureCommand.Execute()).Should().Throw<ArgumentException>("CommandType must be Text. (Parameter 'value')");
			connector.StoredProcedure("values (@two);", ("two", 2)).CommandType.Should().Be(CommandType.StoredProcedure);
		}

		[Test]
		public void TimeoutUnitTests()
		{
			var command = CreateConnector().Command("values (0);");

			command.Timeout.Should().Be(null);

			Invoking(() => command.WithTimeout(TimeSpan.FromSeconds(-10))).Should().Throw<ArgumentOutOfRangeException>();
			Invoking(() => command.WithTimeout(TimeSpan.FromSeconds(0))).Should().Throw<ArgumentOutOfRangeException>();

			var oneMinuteCommand = command.WithTimeout(TimeSpan.FromMinutes(1));
			oneMinuteCommand.Timeout.Should().Be(TimeSpan.FromMinutes(1));
			oneMinuteCommand.Create().CommandTimeout.Should().Be(60);
			var halfSecondCommand = command.WithTimeout(TimeSpan.FromMilliseconds(500));
			halfSecondCommand.Timeout.Should().Be(TimeSpan.FromMilliseconds(500));
			halfSecondCommand.Create().CommandTimeout.Should().Be(1);
			var noTimeoutCommand = command.WithTimeout(Timeout.InfiniteTimeSpan);
			noTimeoutCommand.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
			noTimeoutCommand.Create().CommandTimeout.Should().Be(0);
		}

		[Test, Timeout(15000)]
		public void TimeoutTest()
		{
			var connectionString = new SqliteConnectionStringBuilder { DataSource = nameof(TimeoutTest), Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared }.ConnectionString;
			using var connector1 = DbConnector.Create(new SqliteConnection(connectionString), new DbConnectorSettings { AutoOpen = true });
			using var connector2 = DbConnector.Create(new SqliteConnection(connectionString), new DbConnectorSettings { AutoOpen = true });
			connector1.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
			connector2.Command("insert into Items (Name) values ('xyzzy');").Execute();
			using var transaction1 = connector1.BeginTransaction();
			Invoking(() => connector2.Command("insert into Items (Name) values ('querty');").WithTimeout(TimeSpan.FromSeconds(1)).Execute()).Should().Throw<SqliteException>();
		}

		private static async Task<IReadOnlyList<T>> ToListAsync<T>(IAsyncEnumerable<T> items)
		{
			var list = new List<T>();
			await foreach (var item in items)
				list.Add(item);
			return list;
		}

		private static async Task<T> FirstAsync<T>(IAsyncEnumerable<T> items)
		{
			await foreach (var item in items)
				return item;
			throw new InvalidOperationException();
		}

		private static DbConnector CreateConnector() => DbConnector.Create(
			new SqliteConnection("Data Source=:memory:"),
			new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

		private static string ToUpper(IDataRecord x) => x.Get<string>().ToUpperInvariant();
	}
}
