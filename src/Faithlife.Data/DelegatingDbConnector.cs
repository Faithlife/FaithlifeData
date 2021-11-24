using System.Data;
using Faithlife.Data.SqlFormatting;

namespace Faithlife.Data;

/// <summary>
/// Delegates to an inner connector.
/// </summary>
public class DelegatingDbConnector : DbConnector
{
	/// <summary>
	/// Creates an instance that delegates to the specified connector.
	/// </summary>
	public DelegatingDbConnector(DbConnector inner)
	{
		Inner = inner;
	}

	/// <inheritdoc />
	public override IDbConnection Connection => Inner.Connection;

	/// <inheritdoc />
	public override IDbTransaction? Transaction => Inner.Transaction;

	/// <inheritdoc />
	public override SqlSyntax SqlSyntax => Inner.SqlSyntax;

	/// <inheritdoc />
	public override ValueTask<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default) => Inner.GetConnectionAsync(cancellationToken);

	/// <inheritdoc />
	public override DbConnectionCloser OpenConnection() => Inner.OpenConnection();

	/// <inheritdoc />
	public override ValueTask<DbConnectionCloser> OpenConnectionAsync(CancellationToken cancellationToken = default) => Inner.OpenConnectionAsync(cancellationToken);

	/// <inheritdoc />
	public override DbTransactionDisposer BeginTransaction() => Inner.BeginTransaction();

	/// <inheritdoc />
	public override DbTransactionDisposer BeginTransaction(IsolationLevel isolationLevel) => Inner.BeginTransaction(isolationLevel);

	/// <inheritdoc />
	public override ValueTask<DbTransactionDisposer> BeginTransactionAsync(CancellationToken cancellationToken = default) => Inner.BeginTransactionAsync(cancellationToken);

	/// <inheritdoc />
	public override ValueTask<DbTransactionDisposer> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default) => Inner.BeginTransactionAsync(isolationLevel, cancellationToken);

	/// <inheritdoc />
	public override DbTransactionDisposer AttachTransaction(IDbTransaction transaction) => Inner.AttachTransaction(transaction);

	/// <inheritdoc />
	public override void CommitTransaction() => Inner.CommitTransaction();

	/// <inheritdoc />
	public override ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default) => Inner.CommitTransactionAsync(cancellationToken);

	/// <inheritdoc />
	public override void RollbackTransaction() => Inner.RollbackTransaction();

	/// <inheritdoc />
	public override ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default) => Inner.RollbackTransactionAsync(cancellationToken);

	/// <inheritdoc />
	public override void ReleaseConnection() => Inner.ReleaseConnection();

	/// <inheritdoc />
	public override ValueTask ReleaseConnectionAsync() => Inner.ReleaseConnectionAsync();

	/// <inheritdoc />
	public override void Dispose() => Inner.Dispose();

	/// <inheritdoc />
	public override ValueTask DisposeAsync() => Inner.DisposeAsync();

	/// <inheritdoc />
	protected internal override DbProviderMethods ProviderMethods => Inner.ProviderMethods;

	/// <inheritdoc />
	protected internal override DbCommandCache? CommandCache => Inner.CommandCache;

	/// <summary>
	/// The inner connector.
	/// </summary>
	protected DbConnector Inner { get; }
}
