namespace Faithlife.Data
{
	/// <summary>
	/// Disposes the transaction when disposed.
	/// </summary>
	public abstract class DbTransactionDisposer : IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Disposes the transaction.
		/// </summary>
		public abstract void Dispose();

		/// <summary>
		/// Disposes the transaction.
		/// </summary>
		public abstract ValueTask DisposeAsync();
	}
}
