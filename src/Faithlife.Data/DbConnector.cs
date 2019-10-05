using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	/// <summary>
	/// Encapsulates a database connection and any current transaction.
	/// </summary>
	public abstract class DbConnector : IDisposable
	{
		/// <summary>
		/// Creates a new DbConnector.
		/// </summary>
		/// <param name="connection">The database connection.</param>
		/// <param name="settings">The settings.</param>
		public static DbConnector Create(IDbConnection connection, DbConnectorSettings? settings = null) =>
			new StandardDbConnector(connection, settings ?? s_defaultSettings);

		/// <summary>
		/// The database connection.
		/// </summary>
		public abstract IDbConnection Connection { get; }

		/// <summary>
		/// The current transaction, if any.
		/// </summary>
		public abstract IDbTransaction? Transaction { get; }

		/// <summary>
		/// Special methods provided by the database provider.
		/// </summary>
		public abstract DbProviderMethods ProviderMethods { get; }

		/// <summary>
		/// Returns the database connection.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The database connection, or null if the connector is disposed.</returns>
		/// <remarks>Allows a lazy-open connector to asynchronously open the connection.</remarks>
		public abstract ValueTask<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Opens the connection.
		/// </summary>
		/// <returns>An <see cref="IDisposable" /> that should be disposed when the connection should be closed.</returns>
		public abstract IDisposable OpenConnection();

		/// <summary>
		/// Opens the connection.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>An <see cref="IDisposable" /> that should be disposed when the connection should be closed.</returns>
		public abstract ValueTask<IDisposable> OpenConnectionAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Begins a transaction.
		/// </summary>
		/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
		public abstract IDisposable BeginTransaction();

		/// <summary>
		/// Begins a transaction.
		/// </summary>
		/// <param name="isolationLevel">The isolation level.</param>
		/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
		public abstract IDisposable BeginTransaction(IsolationLevel isolationLevel);

		/// <summary>
		/// Begins a transaction.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
		public abstract ValueTask<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Begins a transaction.
		/// </summary>
		/// <param name="isolationLevel">The isolation level.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
		public abstract ValueTask<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

		/// <summary>
		/// Commits the current transaction.
		/// </summary>
		public abstract void CommitTransaction();

		/// <summary>
		/// Commits the current transaction.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		public abstract ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Rolls back the current transaction.
		/// </summary>
		public abstract void RollbackTransaction();

		/// <summary>
		/// Rolls back the current transaction.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		public abstract ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new command.
		/// </summary>
		/// <param name="text">The text of the command.</param>
		public DbConnectorCommand Command(string text) => new DbConnectorCommand(this, text, default);

		/// <summary>
		/// Creates a new command.
		/// </summary>
		/// <param name="text">The text of the command.</param>
		/// <param name="parameters">The command parameters.</param>
		public DbConnectorCommand Command(string text, DbParameters parameters) => new DbConnectorCommand(this, text, parameters);

		/// <summary>
		/// Creates a new command.
		/// </summary>
		/// <param name="text">The text of the command.</param>
		/// <param name="parameters">The command parameters.</param>
		public DbConnectorCommand Command(string text, params (string Name, object? Value)[] parameters) => new DbConnectorCommand(this, text, DbParameters.Create(parameters));

		/// <summary>
		/// Disposes the connector.
		/// </summary>
		public abstract void Dispose();

		private static readonly DbConnectorSettings s_defaultSettings = new DbConnectorSettings();
	}
}
