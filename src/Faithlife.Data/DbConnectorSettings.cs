using System;
using System.Data;
using Faithlife.Data.SqlFormatting;

namespace Faithlife.Data
{
	/// <summary>
	/// Settings when creating a <see cref="DbConnector"/>.
	/// </summary>
	public class DbConnectorSettings
	{
		/// <summary>
		/// If true, does not actually open the connection until it is needed.
		/// </summary>
		/// <remarks>If this property is true, <see cref="DbConnector.OpenConnection"/> doesn't immediately open the connection,
		/// but rather waits until the Connection property is first accessed.</remarks>
		public bool LazyOpen { get; set; }

		/// <summary>
		/// If true, automatically opens the connection.
		/// </summary>
		/// <remarks>If this property is true, there is no need to call <see cref="DbConnector.OpenConnection"/>. The connection
		/// is opened immediately (or lazily if <see cref="LazyOpen"/> is also specified).</remarks>
		public bool AutoOpen { get; set; }

		/// <summary>
		/// If true, does not dispose the connection when the connector is disposed.
		/// </summary>
		public bool NoDispose { get; set; }

		/// <summary>
		/// The current transaction of the connection.
		/// </summary>
		public IDbTransaction? CurrentTransaction { get; set; }

		/// <summary>
		/// Called when the connector is disposed.
		/// </summary>
		public Action? WhenDisposed { get; set; }

		/// <summary>
		/// Provider-specific database methods.
		/// </summary>
		public DbProviderMethods? ProviderMethods { get; set; }

		/// <summary>
		/// The SQL syntax to use when formatting SQL.
		/// </summary>
		public SqlSyntax? SqlSyntax { get; set; }

		/// <summary>
		/// The isolation level used when <c>BeginTransaction(Async)</c> is called without one.
		/// </summary>
		/// <remarks>If not specified, the behavior is driver-specific.</remarks>
		public IsolationLevel? DefaultIsolationLevel { get; set; }
	}
}
