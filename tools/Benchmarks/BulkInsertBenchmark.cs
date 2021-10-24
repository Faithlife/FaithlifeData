using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Faithlife.Data;
using Faithlife.Data.BulkInsert;
using Faithlife.Data.SqlFormatting;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;

namespace Benchmarks
{
	public abstract class BulkInsertBenchmark : IDisposable
	{
		public class SqliteBulkInsertBenchmark : BulkInsertBenchmark
		{
			public SqliteBulkInsertBenchmark()
				: base(new SqliteConnection("Data Source=:memory:"),
					columnsSql: "ItemId integer primary key, Value integer not null",
					recordCount: 5000)
			{
			}
		}

		public class MySqlBulkInsertBenchmark : BulkInsertBenchmark
		{
			public MySqlBulkInsertBenchmark()
				: base(new MySqlConnection("Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=test;Ignore Prepare=false"),
					columnsSql: "ItemId int not null auto_increment primary key, Value int not null",
					recordCount: 10000)
			{
			}
		}

		public class SqlServerBulkInsertBenchmark : BulkInsertBenchmark
		{
			public SqlServerBulkInsertBenchmark()
				: base(new SqlConnection("data source=localhost;user id=sa;password=P@ssw0rd;initial catalog=test"),
					columnsSql: "ItemId int not null identity primary key, Value int not null",
					recordCount: 5000,
					createParameter: x => new SqlParameter { Value = x, DbType = DbType.Int32 })
			{
			}
		}

		public class NpgsqlBulkInsertBenchmark : BulkInsertBenchmark
		{
			public NpgsqlBulkInsertBenchmark()
				: base(new NpgsqlConnection("host=localhost;user id=root;password=test;database=test"),
					columnsSql: "ItemId serial primary key, Value int not null",
					recordCount: 10000)
			{
			}
		}

		protected BulkInsertBenchmark(IDbConnection connection, string columnsSql, int recordCount, Func<int, object>? createParameter = null)
		{
			m_connector = DbConnector.Create(connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });
			m_connector.Command("drop table if exists BulkInsertBenchmark; ").Execute();
			m_connector.CommandFormat($"create table BulkInsertBenchmark ({Sql.Raw(columnsSql)});").Execute();
			m_recordCount = recordCount;
			m_createParameter = createParameter;

			m_sql = "insert into BulkInsertBenchmark (Value) values (@Value)...;";
		}

		[Benchmark]
		public void Normal() => m_connector.Command(m_sql).BulkInsert(Params());

		[Benchmark]
		public void Cached() => m_connector.Command(m_sql).Cache().BulkInsert(Params());

		[Benchmark]
		public void Prepared() => m_connector.Command(m_sql).Prepare().BulkInsert(Params());

		[Benchmark]
		public void PreparedAndCached() => m_connector.Command(m_sql).Prepare().Cache().BulkInsert(Params());

		public void Dispose() => m_connector.Dispose();

		private IEnumerable<DbParameters> Params() => Enumerable.Range(0, m_recordCount).Select(x => DbParameters.Create("Value", Param(x)));

		private object Param(int x) => m_createParameter is null ? x : m_createParameter(x);

		private readonly DbConnector m_connector;
		private readonly int m_recordCount;
		private readonly Func<int, object>? m_createParameter;
		private readonly string m_sql;
	}
}
