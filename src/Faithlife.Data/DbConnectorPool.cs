using System.Diagnostics.CodeAnalysis;

namespace Faithlife.Data;

/// <summary>
/// Maintains a pool of connectors.
/// </summary>
/// <remarks>This is potentially useful if your ADO.NET provider doesn't do its own connection pooling.
/// The database connection of a pooled connector will retain whatever state it had when it was returned
/// to the pool.</remarks>
public sealed class DbConnectorPool : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Creates a new connector pool.
	/// </summary>
	/// <param name="settings">The settings.</param>
	public DbConnectorPool(DbConnectorPoolSettings settings)
	{
		_ = settings ?? throw new ArgumentNullException(nameof(settings));
		m_create = settings.Create ?? throw new ArgumentException($"{nameof(settings.Create)} is required.");

		m_lock = new object();
		m_idleConnectors = new Stack<DbConnector>();
	}

	/// <summary>
	/// Creates a connector that delegates to a connector in the pool (or a new connector if the pool is empty).
	/// </summary>
	/// <remarks>Dispose the returned connector to return the actual connector to the pool.</remarks>
	public DbConnector Get()
	{
		DbConnector? innerConnector = null;

		lock (m_lock)
		{
			if (m_idleConnectors is null)
				throw new ObjectDisposedException(nameof(DbConnectorPool));
			if (m_idleConnectors.Count != 0)
				innerConnector = m_idleConnectors.Pop();
		}

		return new PooledDbConnector(this, innerConnector ?? m_create());
	}

	/// <summary>
	/// Disposes the connector pool.
	/// </summary>
	/// <seealso cref="DisposeAsync" />
	public void Dispose()
	{
		Stack<DbConnector>? connectors;

		lock (m_lock)
		{
			connectors = m_idleConnectors;
			m_idleConnectors = null;
		}

		if (connectors != null)
		{
			foreach (var connector in connectors)
				connector.Dispose();
		}
	}

	/// <summary>
	/// Disposes the connector pool.
	/// </summary>
	/// <seealso cref="Dispose" />
	public async ValueTask DisposeAsync()
	{
		Stack<DbConnector>? connectors;

		lock (m_lock)
		{
			connectors = m_idleConnectors;
			m_idleConnectors = null;
		}

		if (connectors != null)
		{
			foreach (var connector in connectors)
				await connector.DisposeAsync().ConfigureAwait(false);
		}
	}

	private void ReturnInnerConnector(DbConnector innerConnector)
	{
		lock (m_lock)
		{
			if (m_idleConnectors is null)
				throw new InvalidOperationException($"{nameof(DbConnectorPool)} was disposed.");
			m_idleConnectors.Push(innerConnector);
		}
	}

	private sealed class PooledDbConnector : DelegatingDbConnector
	{
		public PooledDbConnector(DbConnectorPool pool, DbConnector inner)
			: base(inner)
		{
			m_pool = pool;
		}

		[SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "Don't dispose inner connector.")]
		public override void Dispose()
		{
			m_pool?.ReturnInnerConnector(Inner);
			m_pool = null;
		}

		[SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "Don't dispose inner connector.")]
		public override ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}

		private DbConnectorPool? m_pool;
	}

	private readonly Func<DbConnector> m_create;
	private readonly object m_lock;
	private Stack<DbConnector>? m_idleConnectors;
}
