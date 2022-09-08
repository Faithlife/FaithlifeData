using System.Data;

namespace Faithlife.Data;

/// <summary>
/// Delegates to an inner provider.
/// </summary>
public class DelegatingDbProviderMethods : DbProviderMethods
{
	/// <summary>
	/// Creates an instance that delegates to the specified provider.
	/// </summary>
	public DelegatingDbProviderMethods(DbProviderMethods inner)
	{
		Inner = inner;
	}

	/// <inheritdoc />
	public override ValueTask OpenConnectionAsync(IDbConnection connection, CancellationToken cancellationToken) => Inner.OpenConnectionAsync(connection, cancellationToken);

	/// <inheritdoc />
	public override ValueTask CloseConnectionAsync(IDbConnection connection) => Inner.CloseConnectionAsync(connection);

	/// <inheritdoc />
	public override ValueTask DisposeConnectionAsync(IDbConnection connection) => Inner.DisposeConnectionAsync(connection);

	/// <inheritdoc />
	public override ValueTask<IDbTransaction> BeginTransactionAsync(IDbConnection connection, CancellationToken cancellationToken) => Inner.BeginTransactionAsync(connection, cancellationToken);

	/// <inheritdoc />
	public override ValueTask<IDbTransaction> BeginTransactionAsync(IDbConnection connection, IsolationLevel isolationLevel, CancellationToken cancellationToken) => Inner.BeginTransactionAsync(connection, isolationLevel, cancellationToken);

	/// <inheritdoc />
	public override ValueTask CommitTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken) => Inner.CommitTransactionAsync(transaction, cancellationToken);

	/// <inheritdoc />
	public override ValueTask RollbackTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken) => Inner.RollbackTransactionAsync(transaction, cancellationToken);

	/// <inheritdoc />
	public override ValueTask DisposeTransactionAsync(IDbTransaction transaction) => Inner.DisposeTransactionAsync(transaction);

	/// <inheritdoc />
	public override ValueTask<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken) => Inner.ExecuteNonQueryAsync(command, cancellationToken);

	/// <inheritdoc />
	public override ValueTask<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken) => Inner.ExecuteReaderAsync(command, cancellationToken);

	/// <inheritdoc />
	public override ValueTask<IDataReader> ExecuteReaderAsync(IDbCommand command, CommandBehavior commandBehavior, CancellationToken cancellationToken) => Inner.ExecuteReaderAsync(command, commandBehavior, cancellationToken);

	/// <inheritdoc />
	public override ValueTask PrepareCommandAsync(IDbCommand command, CancellationToken cancellationToken) => Inner.PrepareCommandAsync(command, cancellationToken);

	/// <inheritdoc />
	public override ValueTask DisposeCommandAsync(IDbCommand command) => Inner.DisposeCommandAsync(command);

	/// <inheritdoc />
	public override ValueTask<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken) => Inner.ReadAsync(reader, cancellationToken);

	/// <inheritdoc />
	public override ValueTask<bool> NextResultAsync(IDataReader reader, CancellationToken cancellationToken) => Inner.NextResultAsync(reader, cancellationToken);

	/// <inheritdoc />
	public override ValueTask DisposeReaderAsync(IDataReader reader) => Inner.DisposeReaderAsync(reader);

	/// <summary>
	/// The inner provider.
	/// </summary>
	protected DbProviderMethods Inner { get; }
}
