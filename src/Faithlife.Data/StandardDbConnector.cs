using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	internal sealed class StandardDbConnector : DbConnector
	{
		public StandardDbConnector(IDbConnection connection, DbConnectorSettings settings)
		{
			m_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			settings = settings ?? s_defaultSettings;

			m_shouldLazyOpen = settings.LazyOpen;
			m_isConnectionOpen = m_connection.State == ConnectionState.Open;
			m_noCloseConnection = m_isConnectionOpen;
			m_transaction = settings.CurrentTransaction;
			m_noDisposeTransaction = m_transaction != null;
			m_noDisposeConnection = m_noDisposeTransaction || settings.NoDispose;
			m_whenDisposed = settings.WhenDisposed;
			m_methods = new DbProviderMethods();

			if (settings.AutoOpen && !m_isConnectionOpen)
				OpenConnection();
		}

		public override IDbConnection Connection
		{
			get
			{
				if (m_connection != null && m_pendingLazyOpen)
				{
					m_pendingLazyOpen = false;
					m_connection.Open();
				}

				return m_connection;
			}
		}

		public override IDbTransaction Transaction => m_transaction;

		public override async Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken)
		{
			if (m_connection != null && m_pendingLazyOpen)
			{
				m_pendingLazyOpen = false;
				await m_methods.OpenConnectionAsync(m_connection, cancellationToken).ConfigureAwait(false);
			}

			return m_connection;
		}

		public override IDisposable OpenConnection()
		{
			VerifyCanOpenConnection();

			if (m_shouldLazyOpen)
				m_pendingLazyOpen = true;
			else
				m_connection.Open();
			m_isConnectionOpen = true;

			return new ConnectionCloser(this);
		}

		public override async Task<IDisposable> OpenConnectionAsync(CancellationToken cancellationToken)
		{
			VerifyCanOpenConnection();

			if (m_shouldLazyOpen)
				m_pendingLazyOpen = true;
			else
				await m_methods.OpenConnectionAsync(m_connection, cancellationToken).ConfigureAwait(false);
			m_isConnectionOpen = true;

			return new ConnectionCloser(this);
		}

		public override IDisposable BeginTransaction()
		{
			VerifyCanBeginTransaction();
			m_transaction = Connection.BeginTransaction();
			return new TransactionDisposer(this);
		}

		public override IDisposable BeginTransaction(IsolationLevel isolationLevel)
		{
			VerifyCanBeginTransaction();
			m_transaction = Connection.BeginTransaction(isolationLevel);
			return new TransactionDisposer(this);
		}

		public override async Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken)
		{
			VerifyCanBeginTransaction();
			m_transaction = await m_methods.BeginTransactionAsync(Connection, cancellationToken).ConfigureAwait(false);
			return new TransactionDisposer(this);
		}

		public override async Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
		{
			VerifyCanBeginTransaction();
			m_transaction = await m_methods.BeginTransactionAsync(Connection, isolationLevel, cancellationToken).ConfigureAwait(false);
			return new TransactionDisposer(this);
		}

		public override void CommitTransaction()
		{
			VerifyCanEndTransaction();
			m_transaction.Commit();
			DisposeTransaction();
		}

		public override async Task CommitTransactionAsync(CancellationToken cancellationToken)
		{
			VerifyCanEndTransaction();
			await m_methods.CommitTransactionAsync(m_transaction, cancellationToken).ConfigureAwait(false);
			DisposeTransaction();
		}

		public override void RollbackTransaction()
		{
			VerifyCanEndTransaction();
			m_transaction.Rollback();
			DisposeTransaction();
		}

		public override async Task RollbackTransactionAsync(CancellationToken cancellationToken)
		{
			VerifyCanEndTransaction();
			await m_methods.RollbackTransactionAsync(m_transaction, cancellationToken).ConfigureAwait(false);
			DisposeTransaction();
		}

		public override void Dispose()
		{
			DisposeTransaction();

			if (!m_noDisposeConnection)
				m_connection?.Dispose();
			m_connection = null;

			m_whenDisposed?.Invoke();
			m_whenDisposed = null;
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

		private void DisposeTransaction()
		{
			VerifyNotDisposed();

			if (!m_noDisposeTransaction)
				m_transaction?.Dispose();
			m_transaction = null;
		}

		private void VerifyNotDisposed()
		{
			if (m_connection == null)
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

		private void VerifyCanEndTransaction()
		{
			VerifyNotDisposed();

			if (m_transaction == null)
				throw new InvalidOperationException("No transaction available; call BeginTransaction first.");
		}

		private sealed class ConnectionCloser : IDisposable
		{
			public ConnectionCloser(StandardDbConnector connector)
			{
				m_connector = connector;
			}

			public void Dispose()
			{
				m_connector?.CloseConnection();
				m_connector = null;
			}

			private StandardDbConnector m_connector;
		}

		private sealed class TransactionDisposer : IDisposable
		{
			public TransactionDisposer(StandardDbConnector connector)
			{
				m_connector = connector;
			}

			public void Dispose()
			{
				m_connector?.DisposeTransaction();
				m_connector = null;
			}

			private StandardDbConnector m_connector;
		}

		private static readonly DbConnectorSettings s_defaultSettings = new DbConnectorSettings();

		private readonly bool m_noDisposeConnection;
		private readonly bool m_noDisposeTransaction;
		private readonly bool m_noCloseConnection;
		private readonly bool m_shouldLazyOpen;
		private readonly DbProviderMethods m_methods;
		private IDbConnection m_connection;
		private IDbTransaction m_transaction;
		private bool m_pendingLazyOpen;
		private bool m_isConnectionOpen;
		private Action m_whenDisposed;
	}
}
