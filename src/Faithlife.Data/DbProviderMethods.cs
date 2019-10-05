using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	/// <summary>
	/// Provides provider-specific database access methods.
	/// </summary>
	public class DbProviderMethods
	{
		/// <summary>
		/// The default database access methods.
		/// </summary>
		public static readonly DbProviderMethods Default = new DbProviderMethods();

		/// <summary>
		/// Opens the connection asynchronously.
		/// </summary>
		public virtual ValueTask OpenConnectionAsync(IDbConnection connection, CancellationToken cancellationToken)
		{
			if (connection is DbConnection dbConnection)
				return new ValueTask(dbConnection.OpenAsync(cancellationToken));

			connection.Open();
			return new ValueTask();
		}

		/// <summary>
		/// Disposes a transaction asynchronously.
		/// </summary>
		public virtual ValueTask DisposeConnectionAsync(IDbConnection connection)
		{
#if NETSTANDARD2_1
			if (connection is DbConnection dbConnection)
				return dbConnection.DisposeAsync();
#endif

			connection.Dispose();
			return new ValueTask();
		}

		/// <summary>
		/// Begins a transaction asynchronously.
		/// </summary>
		public virtual ValueTask<IDbTransaction> BeginTransactionAsync(IDbConnection connection, CancellationToken cancellationToken)
		{
#if NETSTANDARD2_1
			if (connection is DbConnection dbConnection)
			{
				static async ValueTask<IDbTransaction> doAsync(DbConnection c, CancellationToken ct) =>
					await c.BeginTransactionAsync(ct).ConfigureAwait(false);
				return doAsync(dbConnection, cancellationToken);
			}
#endif

			return new ValueTask<IDbTransaction>(connection.BeginTransaction());
		}

		/// <summary>
		/// Begins a transaction asynchronously.
		/// </summary>
		public virtual ValueTask<IDbTransaction> BeginTransactionAsync(IDbConnection connection, IsolationLevel isolationLevel, CancellationToken cancellationToken)
		{
#if NETSTANDARD2_1
			if (connection is DbConnection dbConnection)
			{
				static async ValueTask<IDbTransaction> doAsync(DbConnection c, IsolationLevel il, CancellationToken ct) =>
					await c.BeginTransactionAsync(il, ct).ConfigureAwait(false);
				return doAsync(dbConnection, isolationLevel, cancellationToken);
			}
#endif

			return new ValueTask<IDbTransaction>(connection.BeginTransaction(isolationLevel));
		}

		/// <summary>
		/// Commits a transaction asynchronously.
		/// </summary>
		public virtual ValueTask CommitTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken)
		{
#if NETSTANDARD2_1
			if (transaction is DbTransaction dbTransaction)
				return new ValueTask(dbTransaction.CommitAsync(cancellationToken));
#endif

			transaction.Commit();
			return new ValueTask();
		}

		/// <summary>
		/// Rolls back a transaction asynchronously.
		/// </summary>
		public virtual ValueTask RollbackTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken)
		{
#if NETSTANDARD2_1
			if (transaction is DbTransaction dbTransaction)
				return new ValueTask(dbTransaction.RollbackAsync(cancellationToken));
#endif

			transaction.Rollback();
			return new ValueTask();
		}

		/// <summary>
		/// Disposes a transaction asynchronously.
		/// </summary>
		public virtual ValueTask DisposeTransactionAsync(IDbTransaction transaction)
		{
#if NETSTANDARD2_1
			if (transaction is DbTransaction dbTransaction)
				return dbTransaction.DisposeAsync();
#endif

			transaction.Dispose();
			return new ValueTask();
		}

		/// <summary>
		/// Executes a non-query command asynchronously.
		/// </summary>
		public virtual ValueTask<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
		{
			if (command is DbCommand dbCommand)
				return new ValueTask<int>(dbCommand.ExecuteNonQueryAsync(cancellationToken));

			return new ValueTask<int>(command.ExecuteNonQuery());
		}

		/// <summary>
		/// Executes a command query asynchronously.
		/// </summary>
		public virtual ValueTask<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
		{
			if (command is DbCommand dbCommand)
			{
				static async ValueTask<IDataReader> doAsync(DbCommand c, CancellationToken ct) =>
					await c.ExecuteReaderAsync(ct).ConfigureAwait(false);
				return doAsync(dbCommand, cancellationToken);
			}

			return new ValueTask<IDataReader>(command.ExecuteReader());
		}

		/// <summary>
		/// Executes a command query asynchronously.
		/// </summary>
		public virtual ValueTask<IDataReader> ExecuteReaderAsync(IDbCommand command, CommandBehavior commandBehavior, CancellationToken cancellationToken)
		{
			if (command is DbCommand dbCommand)
			{
				static async ValueTask<IDataReader> doAsync(DbCommand c, CommandBehavior cb, CancellationToken ct) =>
					await c.ExecuteReaderAsync(cb, ct).ConfigureAwait(false);
				return doAsync(dbCommand, commandBehavior, cancellationToken);
			}

			return new ValueTask<IDataReader>(command.ExecuteReader(commandBehavior));
		}

		/// <summary>
		/// Reads the next record asynchronously.
		/// </summary>
		public virtual ValueTask<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
		{
			if (reader is DbDataReader dbReader)
				return new ValueTask<bool>(dbReader.ReadAsync(cancellationToken));

			return new ValueTask<bool>(reader.Read());
		}

		/// <summary>
		/// Reads the next result asynchronously.
		/// </summary>
		public virtual ValueTask<bool> NextResultAsync(IDataReader reader, CancellationToken cancellationToken)
		{
			if (reader is DbDataReader dbReader)
				return new ValueTask<bool>(dbReader.NextResultAsync(cancellationToken));

			return new ValueTask<bool>(reader.NextResult());
		}

		/// <summary>
		/// Creates an instance.
		/// </summary>
		protected DbProviderMethods()
		{
		}
	}
}
