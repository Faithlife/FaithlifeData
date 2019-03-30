using System;
using System.Data;

namespace Faithlife.Data
{
	/// <summary>
	/// Settings when creating a <see cref="DbConnector"/>.
	/// </summary>
	public sealed class DbConnectorSettings
	{
		/// <summary>
		/// Do not actually open the connection until it is needed.
		/// </summary>
		/// <remarks>If this flag is set, <see cref="DbConnector.OpenConnection"/> doesn't immediately open the connection,
		/// but rather waits until the Connection property is first accessed.</remarks>
		public bool LazyOpen { get; set; }

		/// <summary>
		/// Automatically opens the connection.
		/// </summary>
		/// <remarks>If this flag is set, there is no need to call <see cref="DbConnector.OpenConnection"/>. The connection
		/// is opened immediately (or lazily if <see cref="LazyOpen"/> is also specified).</remarks>
		public bool AutoOpen { get; set; }

		/// <summary>
		/// Do not dispose the connection when the connector is disposed.
		/// </summary>
		public bool NoDispose { get; set; }

		/// <summary>
		/// The current transaction of the connection.
		/// </summary>
		public IDbTransaction CurrentTransaction { get; set; }

		/// <summary>
		/// Called when the connector is disposed.
		/// </summary>
		public Action WhenDisposed { get; set; }
	}
}
