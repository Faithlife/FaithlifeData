using System;

namespace Faithlife.Data.SqlText
{
	/// <summary>
	/// Controls how SQL strings are rendered.
	/// </summary>
	public abstract class SqlRenderer
	{
		/// <summary>
		/// The default renderer.
		/// </summary>
		public static readonly SqlRenderer Default = new DefaultSqlRenderer();

		/// <summary>
		/// Renders SQL as text and parameters.
		/// </summary>
		public (string Text, DbParameters Parameters) Render(Sql sql)
		{
			var context = new SqlContext(this);
			var text = (sql ?? throw new ArgumentNullException(nameof(sql))).Render(context);
			return (text, context.Parameters);
		}

		private sealed class DefaultSqlRenderer : SqlRenderer
		{
		}
	}
}
