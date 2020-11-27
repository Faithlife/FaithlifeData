namespace Faithlife.Data.SqlText
{
	internal sealed class SqlContext
	{
		public SqlContext(SqlRenderer renderer)
		{
			Renderer = renderer;
		}

		public SqlRenderer Renderer { get; }

		public DbParameters Parameters => DbParameters.Empty;
	}
}
