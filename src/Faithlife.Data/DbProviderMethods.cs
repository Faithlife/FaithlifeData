using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	public class DbProviderMethods
	{
		public static readonly DbProviderMethods Default = new DbProviderMethods();

		public virtual async Task OpenConnectionAsync(IDbConnection connection, CancellationToken cancellationToken)
		{
			if (connection is DbConnection dbConnection)
				await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
			else
				connection.Open();
		}

		public virtual async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection, CancellationToken cancellationToken)
		{
			return connection.BeginTransaction();
		}

		public virtual async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection, IsolationLevel isolationLevel, CancellationToken cancellationToken)
		{
			return connection.BeginTransaction(isolationLevel);
		}

		public virtual async Task CommitTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken)
		{
			transaction.Commit();
		}

		public virtual async Task RollbackTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken)
		{
			transaction.Rollback();
		}

		public virtual async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
		{
			if (command is DbCommand dbCommand)
				return await dbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			else
				return command.ExecuteNonQuery();
		}

		public virtual async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
		{
			if (command is DbCommand dbCommand)
				return await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
			else
				return command.ExecuteReader();
		}

		public virtual async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CommandBehavior commandBehavior, CancellationToken cancellationToken)
		{
			if (command is DbCommand dbCommand)
				return await dbCommand.ExecuteReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
			else
				return command.ExecuteReader(commandBehavior);
		}

		public virtual async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
		{
			if (reader is DbDataReader dbReader)
				return await dbReader.ReadAsync(cancellationToken).ConfigureAwait(false);
			else
				return reader.Read();
		}

		public virtual async Task<bool> NextResultAsync(IDataReader reader, CancellationToken cancellationToken)
		{
			if (reader is DbDataReader dbReader)
				return await dbReader.NextResultAsync(cancellationToken).ConfigureAwait(false);
			else
				return reader.NextResult();
		}

		protected DbProviderMethods()
		{
		}
	}
}
