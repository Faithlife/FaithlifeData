using System.Data;

namespace Faithlife.Data;

/// <summary>
/// A cache of <see cref="IDbCommand" /> by command text.
/// </summary>
public abstract class DbCommandCache
{
	/// <summary>
	/// Creates a new command cache.
	/// </summary>
	public static DbCommandCache Create() => new DictionaryCache();

	/// <summary>
	/// Provides access via connector. Used when creating wrapping connectors.
	/// </summary>
	public static DbCommandCache? FromConnector(DbConnector connector) => connector.CommandCache;

	/// <summary>
	/// Gets the specified cached command, if any.
	/// </summary>
	protected internal abstract bool TryGetCommand(string text, out IDbCommand command);

	/// <summary>
	/// Adds the specified command to the cache.
	/// </summary>
	protected internal abstract void AddCommand(string text, IDbCommand command);

	/// <summary>
	/// Gets all of the cached commands.
	/// </summary>
	protected internal abstract IReadOnlyCollection<IDbCommand> GetCommands();

	private sealed class DictionaryCache : DbCommandCache
	{
		protected internal override bool TryGetCommand(string text, out IDbCommand command) => m_dictionary.TryGetValue(text, out command);

		protected internal override void AddCommand(string text, IDbCommand command) => m_dictionary.Add(text, command);

		protected internal override IReadOnlyCollection<IDbCommand> GetCommands() => m_dictionary.Values;

		private readonly Dictionary<string, IDbCommand> m_dictionary = new();
	}
}
