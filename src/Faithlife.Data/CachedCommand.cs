using System.Data;

namespace Faithlife.Data
{
	/// <summary>
	/// Wraps a cached command.
	/// </summary>
	/// <remarks>This class makes <c>Dispose</c> a no-op so that the inner command
	/// can be reused. It also prevents modifications to the command that could leak to
	/// the next consumer of the command.</remarks>
	internal sealed class CachedCommand : IDbCommand
	{
		public CachedCommand(IDbCommand inner) => Inner = inner;

		public static IDbCommand Unwrap(IDbCommand command) =>
			command is CachedCommand cachedCommand ? cachedCommand.Inner : command;

		public IDbCommand Inner { get; }

		public void Dispose()
		{
		}

		public void Cancel() => Inner.Cancel();

		public IDbDataParameter CreateParameter() => Inner.CreateParameter();

		public int ExecuteNonQuery() => Inner.ExecuteNonQuery();

		public IDataReader ExecuteReader() => Inner.ExecuteReader();

		public IDataReader ExecuteReader(CommandBehavior behavior) => Inner.ExecuteReader(behavior);

		public object ExecuteScalar() => Inner.ExecuteScalar();

		public void Prepare() => Inner.Prepare();

		public string CommandText
		{
			get => Inner.CommandText;
			set => throw CreateException();
		}

		public int CommandTimeout
		{
			get => Inner.CommandTimeout;
			set => throw CreateException();
		}

		public CommandType CommandType
		{
			get => Inner.CommandType;
			set => throw CreateException();
		}

		public IDbConnection Connection
		{
			get => Inner.Connection;
			set => throw CreateException();
		}

		public IDataParameterCollection Parameters => Inner.Parameters;

		public IDbTransaction Transaction
		{
			get => Inner.Transaction;
			set => Inner.Transaction = value;
		}

		public UpdateRowSource UpdatedRowSource
		{
			get => Inner.UpdatedRowSource;
			set => throw CreateException();
		}

		private static InvalidOperationException CreateException() =>
			new InvalidOperationException("Property cannot be modified for cached command.");
	}
}
