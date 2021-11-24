namespace Faithlife.Data;

/// <summary>
/// Settings when creating a <see cref="DbConnectorPool"/>.
/// </summary>
public class DbConnectorPoolSettings
{
	/// <summary>
	/// Creates a new connector for the pool.
	/// </summary>
	/// <remarks>The created connector is wrapped in a connector that stores the actual connector
	/// in the pool when disposed. Since the advantage of a connector pool is keeping database
	/// connections open, be sure to use <see cref="DbConnectorSettings.AutoOpen"/> (and optionally
	/// <see cref="DbConnectorSettings.LazyOpen"/>) when creating the connector.</remarks>
	public Func<DbConnector>? Create { get; set; }
}
