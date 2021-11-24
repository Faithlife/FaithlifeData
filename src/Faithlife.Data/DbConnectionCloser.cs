namespace Faithlife.Data
{
	/// <summary>
	/// Closes the connection when disposed.
	/// </summary>
	public abstract class DbConnectionCloser : IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Closes the connection.
		/// </summary>
		public abstract void Dispose();

		/// <summary>
		/// Closes the connection.
		/// </summary>
		public abstract ValueTask DisposeAsync();
	}
}
