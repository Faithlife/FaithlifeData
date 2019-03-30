using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	public class DbProviderMethods
	{
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
	}
}
