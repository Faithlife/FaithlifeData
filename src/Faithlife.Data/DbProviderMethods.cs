using System.Data;
using System.Data.Common;

namespace Faithlife.Data;

/// <summary>
/// Provides provider-specific database access methods.
/// </summary>
public class DbProviderMethods
{
	/// <summary>
	/// The default database access methods.
	/// </summary>
	public static readonly DbProviderMethods Default = new();

	/// <summary>
	/// Provides access via connector. Used when creating wrapping connectors.
	/// </summary>
	public static DbProviderMethods FromConnector(DbConnector connector) => connector.ProviderMethods;

	/// <summary>
	/// Opens the connection.
	/// </summary>
	public virtual void OpenConnection(IDbConnection connection) => connection.Open();

	/// <summary>
	/// Opens the connection asynchronously.
	/// </summary>
	public virtual ValueTask OpenConnectionAsync(IDbConnection connection, CancellationToken cancellationToken)
	{
		if (connection is DbConnection dbConnection)
			return new ValueTask(dbConnection.OpenAsync(cancellationToken));

		connection.Open();
		return default;
	}

	/// <summary>
	/// Closes a connection asynchronously.
	/// </summary>
	public virtual ValueTask CloseConnectionAsync(IDbConnection connection)
	{
#if !NETSTANDARD2_0
		if (connection is DbConnection dbConnection)
			return new ValueTask(dbConnection.CloseAsync());
#endif

		connection.Close();
		return default;
	}

	/// <summary>
	/// Disposes a connection asynchronously.
	/// </summary>
	public virtual ValueTask DisposeConnectionAsync(IDbConnection connection)
	{
#if !NETSTANDARD2_0
		if (connection is DbConnection dbConnection)
			return dbConnection.DisposeAsync();
#endif

		connection.Dispose();
		return default;
	}

	/// <summary>
	/// Begins a transaction asynchronously.
	/// </summary>
	public virtual ValueTask<IDbTransaction> BeginTransactionAsync(IDbConnection connection, CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (connection is DbConnection dbConnection)
		{
			static async ValueTask<IDbTransaction> DoAsync(DbConnection c, CancellationToken ct) =>
				await c.BeginTransactionAsync(ct).ConfigureAwait(false);

			return DoAsync(dbConnection, cancellationToken);
		}
#endif

		return new ValueTask<IDbTransaction>(connection.BeginTransaction());
	}

	/// <summary>
	/// Begins a transaction asynchronously.
	/// </summary>
	public virtual ValueTask<IDbTransaction> BeginTransactionAsync(IDbConnection connection, IsolationLevel isolationLevel, CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (connection is DbConnection dbConnection)
		{
			static async ValueTask<IDbTransaction> DoAsync(DbConnection c, IsolationLevel il, CancellationToken ct) =>
				await c.BeginTransactionAsync(il, ct).ConfigureAwait(false);

			return DoAsync(dbConnection, isolationLevel, cancellationToken);
		}
#endif

		return new ValueTask<IDbTransaction>(connection.BeginTransaction(isolationLevel));
	}

	/// <summary>
	/// Commits a transaction asynchronously.
	/// </summary>
	public virtual ValueTask CommitTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (transaction is DbTransaction dbTransaction)
			return new ValueTask(dbTransaction.CommitAsync(cancellationToken));
#endif

		transaction.Commit();
		return default;
	}

	/// <summary>
	/// Rolls back a transaction asynchronously.
	/// </summary>
	public virtual ValueTask RollbackTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (transaction is DbTransaction dbTransaction)
			return new ValueTask(dbTransaction.RollbackAsync(cancellationToken));
#endif

		transaction.Rollback();
		return default;
	}

	/// <summary>
	/// Disposes a transaction asynchronously.
	/// </summary>
	public virtual ValueTask DisposeTransactionAsync(IDbTransaction transaction)
	{
#if !NETSTANDARD2_0
		if (transaction is DbTransaction dbTransaction)
			return dbTransaction.DisposeAsync();
#endif

		transaction.Dispose();
		return default;
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
			static async ValueTask<IDataReader> DoAsync(DbCommand c, CancellationToken ct) =>
				await c.ExecuteReaderAsync(ct).ConfigureAwait(false);

			return DoAsync(dbCommand, cancellationToken);
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
			static async ValueTask<IDataReader> DoAsync(DbCommand c, CommandBehavior cb, CancellationToken ct) =>
				await c.ExecuteReaderAsync(cb, ct).ConfigureAwait(false);

			return DoAsync(dbCommand, commandBehavior, cancellationToken);
		}

		return new ValueTask<IDataReader>(command.ExecuteReader(commandBehavior));
	}

	/// <summary>
	/// Prepares a command asynchronously.
	/// </summary>
	public virtual ValueTask PrepareCommandAsync(IDbCommand command, CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (command is DbCommand dbCommand)
			return new ValueTask(dbCommand.PrepareAsync(cancellationToken));
#endif

		command.Prepare();
		return default;
	}

	/// <summary>
	/// Disposes a command asynchronously.
	/// </summary>
	public virtual ValueTask DisposeCommandAsync(IDbCommand command)
	{
#if !NETSTANDARD2_0
		if (command is DbCommand dbCommand)
			return dbCommand.DisposeAsync();
#endif

		command.Dispose();
		return default;
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
	/// Disposes a reader asynchronously.
	/// </summary>
	public virtual ValueTask DisposeReaderAsync(IDataReader reader)
	{
#if !NETSTANDARD2_0
		if (reader is DbDataReader dbReader)
			return dbReader.DisposeAsync();
#endif

		reader.Dispose();
		return default;
	}

	/// <summary>
	/// Creates an instance.
	/// </summary>
	protected DbProviderMethods()
	{
	}
}
