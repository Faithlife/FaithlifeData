using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data.Tests
{
	public sealed class SqliteProviderMethods : DbProviderMethods
	{
		public override ValueTask OpenConnectionAsync(IDbConnection connection, CancellationToken cancellationToken)
		{
			connection.Open();
			return new ValueTask();
		}
	}
}
