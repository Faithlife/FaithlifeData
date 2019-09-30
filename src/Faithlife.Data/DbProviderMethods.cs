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
		public virtual async Task OpenConnectionAsync(IDbConnection connection, CancellationToken cancellationToken)
		{
			if (connection is DbConnection dbConnection)
				await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
			else
				connection.Open();
		}

		/// <summary>
		/// Begins a transaction asynchronously.
		/// </summary>
		public virtual async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection, CancellationToken cancellationToken) =>
			connection.BeginTransaction();

		/// <summary>
		/// Begins a transaction asynchronously.
		/// </summary>
		public virtual async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection, IsolationLevel isolationLevel, CancellationToken cancellationToken) =>
			connection.BeginTransaction(isolationLevel);

		/// <summary>
		/// Commits a transaction asynchronously.
		/// </summary>
		public virtual async Task CommitTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken) =>
			transaction.Commit();

		/// <summary>
		/// Rolls back a transaction asynchronously.
		/// </summary>
		public virtual async Task RollbackTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken) =>
			transaction.Rollback();

		/// <summary>
		/// Executes a non-query command asynchronously.
		/// </summary>
		public virtual async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
		{
			if (command is DbCommand dbCommand)
				return await dbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			else
				return command.ExecuteNonQuery();
		}

		/// <summary>
		/// Executes a command query asynchronously.
		/// </summary>
		public virtual async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
		{
			if (command is DbCommand dbCommand)
				return await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
			else
				return command.ExecuteReader();
		}

		/// <summary>
		/// Executes a command query asynchronously.
		/// </summary>
		public virtual async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CommandBehavior commandBehavior, CancellationToken cancellationToken)
		{
			if (command is DbCommand dbCommand)
				return await dbCommand.ExecuteReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
			else
				return command.ExecuteReader(commandBehavior);
		}

		/// <summary>
		/// Reads the next record asynchronously.
		/// </summary>
		public virtual async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
		{
			if (reader is DbDataReader dbReader)
				return await dbReader.ReadAsync(cancellationToken).ConfigureAwait(false);
			else
				return reader.Read();
		}

		/// <summary>
		/// Reads the next result asynchronously.
		/// </summary>
		public virtual async Task<bool> NextResultAsync(IDataReader reader, CancellationToken cancellationToken)
		{
			if (reader is DbDataReader dbReader)
				return await dbReader.NextResultAsync(cancellationToken).ConfigureAwait(false);
			else
				return reader.NextResult();
		}

		/// <summary>
		/// Creates an instance.
		/// </summary>
		protected DbProviderMethods()
		{
		}
	}
}
