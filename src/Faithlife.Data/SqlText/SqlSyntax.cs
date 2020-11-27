using System;

namespace Faithlife.Data.SqlText
{
	/// <summary>
	/// The syntax used by a particular SQL dialect.
	/// </summary>
	public abstract class SqlSyntax
	{
		/// <summary>
		/// The default syntax.
		/// </summary>
		public static readonly SqlSyntax Default = new DefaultSqlSyntax();

		/// <summary>
		/// The prefix for named parameters.
		/// </summary>
		public char ParameterPrefix { get; } = '@';

		/// <summary>
		/// Renders SQL as text and parameters.
		/// </summary>
		public (string Text, DbParameters Parameters) Render(Sql sql)
		{
			var context = new SqlContext(this);
			var text = (sql ?? throw new ArgumentNullException(nameof(sql))).Render(context);
			return (text, context.Parameters);
		}

		private sealed class DefaultSqlSyntax : SqlSyntax
		{
		}
	}
}
