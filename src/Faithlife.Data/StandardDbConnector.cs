using System.Data;
using Faithlife.Data.SqlFormatting;

namespace Faithlife.Data;

internal sealed class StandardDbConnector : DbConnector
{
	public StandardDbConnector(IDbConnection connection, DbConnectorSettings settings)
	{
		m_connection = connection ?? throw new ArgumentNullException(nameof(connection));

		m_shouldLazyOpen = settings.LazyOpen;
		m_isConnectionOpen = m_connection.State == ConnectionState.Open;
		m_noCloseConnection = m_isConnectionOpen;
		m_transaction = settings.CurrentTransaction;
		m_noDisposeTransaction = m_transaction != null;
		m_noDisposeConnection = m_noDisposeTransaction || settings.NoDispose;
		m_whenDisposed = settings.WhenDisposed;
		m_providerMethods = settings.ProviderMethods ?? DbProviderMethods.Default;
		m_defaultIsolationLevel = settings.DefaultIsolationLevel;
		SqlSyntax = settings.SqlSyntax ?? SqlSyntax.Default;

		if (settings.AutoOpen && !m_isConnectionOpen)
			OpenConnection();
	}

	public override IDbConnection Connection
	{
		get
		{
			VerifyNotDisposed();
			return m_pendingLazyOpen ? LazyOpenConnection() : m_connection;
		}
	}

	public override IDbTransaction? Transaction => m_transaction;

	public override SqlSyntax SqlSyntax { get; }

	public override ValueTask<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
	{
		VerifyNotDisposed();
		return m_pendingLazyOpen ? LazyOpenConnectionAsync(cancellationToken) : new ValueTask<IDbConnection>(m_connection);
	}

	public override DbConnectionCloser OpenConnection()
	{
		VerifyCanOpenConnection();

		if (m_shouldLazyOpen)
			m_pendingLazyOpen = true;
		else
			OpenDbConnection(m_connection);
		m_isConnectionOpen = true;

		return new ConnectionCloser(this);
	}

	public override async ValueTask<DbConnectionCloser> OpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		VerifyCanOpenConnection();

		if (m_shouldLazyOpen)
			m_pendingLazyOpen = true;
		else
			await OpenDbConnectionAsync(m_connection, cancellationToken).ConfigureAwait(false);
		m_isConnectionOpen = true;

		return new ConnectionCloser(this);
	}

	public override DbTransactionDisposer BeginTransaction()
	{
		VerifyCanBeginTransaction();
		m_transaction = m_defaultIsolationLevel is IsolationLevel isolationLevel
			? Connection.BeginTransaction(isolationLevel)
			: Connection.BeginTransaction();
		return new TransactionDisposer(this);
	}

	public override DbTransactionDisposer BeginTransaction(IsolationLevel isolationLevel)
	{
		VerifyCanBeginTransaction();
		m_transaction = Connection.BeginTransaction(isolationLevel);
		return new TransactionDisposer(this);
	}

	public override async ValueTask<DbTransactionDisposer> BeginTransactionAsync(CancellationToken cancellationToken = default)
	{
		VerifyCanBeginTransaction();
		m_transaction = m_defaultIsolationLevel is IsolationLevel isolationLevel
			? await m_providerMethods.BeginTransactionAsync(Connection, isolationLevel, cancellationToken).ConfigureAwait(false)
			: await m_providerMethods.BeginTransactionAsync(Connection, cancellationToken).ConfigureAwait(false);
		return new TransactionDisposer(this);
	}

	public override async ValueTask<DbTransactionDisposer> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
	{
		VerifyCanBeginTransaction();
		m_transaction = await m_providerMethods.BeginTransactionAsync(Connection, isolationLevel, cancellationToken).ConfigureAwait(false);
		return new TransactionDisposer(this);
	}

	public override DbTransactionDisposer AttachTransaction(IDbTransaction transaction)
	{
		VerifyCanBeginTransaction();
		m_transaction = transaction;
		return new TransactionDisposer(this);
	}

	public override void CommitTransaction()
	{
		VerifyGetTransaction().Commit();
		DisposeTransaction();
	}

	public override async ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default)
	{
		await m_providerMethods.CommitTransactionAsync(VerifyGetTransaction(), cancellationToken).ConfigureAwait(false);
		await DisposeTransactionAsync().ConfigureAwait(false);
	}

	public override void RollbackTransaction()
	{
		VerifyGetTransaction().Rollback();
		DisposeTransaction();
	}

	public override async ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default)
	{
		await m_providerMethods.RollbackTransactionAsync(VerifyGetTransaction(), cancellationToken).ConfigureAwait(false);
		await DisposeTransactionAsync().ConfigureAwait(false);
	}

	public override void ReleaseConnection()
	{
		VerifyNotDisposed();

		if (!m_isConnectionOpen)
			throw new InvalidOperationException("Connection must be open.");

		if (!m_pendingLazyOpen)
		{
			if (!m_noCloseConnection)
				m_connection.Close();
			m_pendingLazyOpen = true;
		}
	}

	public override async ValueTask ReleaseConnectionAsync()
	{
		VerifyNotDisposed();

		if (!m_isConnectionOpen)
			throw new InvalidOperationException("Connection must be open.");

		if (!m_pendingLazyOpen)
		{
			if (!m_noCloseConnection)
				await m_providerMethods.CloseConnectionAsync(m_connection).ConfigureAwait(false);
			m_pendingLazyOpen = true;
		}
	}

	public override void Dispose()
	{
		if (!m_isDisposed)
		{
			DisposeTransaction();

			DisposeCachedCommands();

			if (!m_noDisposeConnection)
				m_connection.Dispose();

			m_whenDisposed?.Invoke();

			m_isDisposed = true;
		}
	}

	public override async ValueTask DisposeAsync()
	{
		if (!m_isDisposed)
		{
			await DisposeTransactionAsync().ConfigureAwait(false);

			await DisposeCachedCommandsAsync().ConfigureAwait(false);

			if (!m_noDisposeConnection)
				await m_providerMethods.DisposeConnectionAsync(m_connection).ConfigureAwait(false);

			m_whenDisposed?.Invoke();

			m_isDisposed = true;
		}
	}

	protected internal override DbProviderMethods ProviderMethods => m_providerMethods;

	protected internal override DbCommandCache CommandCache => m_commandCache ??= DbCommandCache.Create();

	private void OpenDbConnection(IDbConnection dbConnection) => dbConnection.Open();

	private async Task OpenDbConnectionAsync(IDbConnection dbConnection, CancellationToken cancellationToken) => await m_providerMethods.OpenConnectionAsync(dbConnection, cancellationToken).ConfigureAwait(false);

	private IDbConnection LazyOpenConnection()
	{
		m_pendingLazyOpen = false;
		OpenDbConnection(m_connection);
		return m_connection;
	}

	private async ValueTask<IDbConnection> LazyOpenConnectionAsync(CancellationToken cancellationToken)
	{
		m_pendingLazyOpen = false;
		await OpenDbConnectionAsync(m_connection, cancellationToken).ConfigureAwait(false);
		return m_connection;
	}

	private void CloseConnection()
	{
		VerifyNotDisposed();

		if (!m_isConnectionOpen)
			throw new InvalidOperationException("Connection must be open.");

		if (m_pendingLazyOpen)
			m_pendingLazyOpen = false;
		else if (!m_noCloseConnection)
			m_connection.Close();

		m_isConnectionOpen = false;
	}

	private async ValueTask CloseConnectionAsync()
	{
		VerifyNotDisposed();

		if (!m_isConnectionOpen)
			throw new InvalidOperationException("Connection must be open.");

		if (m_pendingLazyOpen)
			m_pendingLazyOpen = false;
		else if (!m_noCloseConnection)
			await m_providerMethods.CloseConnectionAsync(m_connection).ConfigureAwait(false);

		m_isConnectionOpen = false;
	}

	private void DisposeTransaction()
	{
		VerifyNotDisposed();

		if (!m_noDisposeTransaction)
			m_transaction?.Dispose();
		m_transaction = null;
	}

	private async ValueTask DisposeTransactionAsync()
	{
		VerifyNotDisposed();

		if (!m_noDisposeTransaction && m_transaction != null)
			await m_providerMethods.DisposeTransactionAsync(m_transaction).ConfigureAwait(false);
		m_transaction = null;
	}

	private void DisposeCachedCommands()
	{
		if (m_commandCache != null)
		{
			foreach (var command in m_commandCache.GetCommands())
				CachedCommand.Unwrap(command).Dispose();
		}
	}

	private async ValueTask DisposeCachedCommandsAsync()
	{
		if (m_commandCache != null)
		{
			foreach (var command in m_commandCache.GetCommands())
				await m_providerMethods.DisposeCommandAsync(CachedCommand.Unwrap(command)).ConfigureAwait(false);
		}
	}

	private void VerifyNotDisposed()
	{
		if (m_isDisposed)
			throw new ObjectDisposedException(typeof(StandardDbConnector).ToString());
	}

	private void VerifyCanOpenConnection()
	{
		VerifyNotDisposed();

		if (m_isConnectionOpen)
			throw new InvalidOperationException("Connection is already open.");
	}

	private void VerifyCanBeginTransaction()
	{
		VerifyNotDisposed();

		if (!m_isConnectionOpen)
			throw new InvalidOperationException("Connection must be open.");
		if (m_transaction != null)
			throw new InvalidOperationException("A transaction is already started.");
	}

	private IDbTransaction VerifyGetTransaction()
	{
		VerifyNotDisposed();

		if (m_transaction == null)
			throw new InvalidOperationException("No transaction available; call BeginTransaction first.");

		return m_transaction;
	}

	private sealed class ConnectionCloser : DbConnectionCloser
	{
		public ConnectionCloser(StandardDbConnector connector) => m_connector = connector;

		public override void Dispose()
		{
			if (m_connector != null)
			{
				m_connector.CloseConnection();
				m_connector = null;
			}
		}

		public override async ValueTask DisposeAsync()
		{
			if (m_connector != null)
			{
				await m_connector.CloseConnectionAsync().ConfigureAwait(false);
				m_connector = null;
			}
		}

		private StandardDbConnector? m_connector;
	}

	private sealed class TransactionDisposer : DbTransactionDisposer
	{
		public TransactionDisposer(StandardDbConnector connector) => m_connector = connector;

		public override void Dispose()
		{
			if (m_connector != null)
			{
				m_connector.DisposeTransaction();
				m_connector = null;
			}
		}

		public override async ValueTask DisposeAsync()
		{
			if (m_connector != null)
			{
				await m_connector.DisposeTransactionAsync().ConfigureAwait(false);
				m_connector = null;
			}
		}

		private StandardDbConnector? m_connector;
	}

	private readonly bool m_noDisposeConnection;
	private readonly bool m_noDisposeTransaction;
	private readonly bool m_noCloseConnection;
	private readonly bool m_shouldLazyOpen;
	private readonly DbProviderMethods m_providerMethods;
	private readonly IsolationLevel? m_defaultIsolationLevel;
	private readonly Action? m_whenDisposed;
	private readonly IDbConnection m_connection;
	private IDbTransaction? m_transaction;
	private DbCommandCache? m_commandCache;
	private bool m_pendingLazyOpen;
	private bool m_isConnectionOpen;
	private bool m_isDisposed;
}
