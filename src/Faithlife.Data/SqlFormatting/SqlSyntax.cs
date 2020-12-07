using System;

namespace Faithlife.Data.SqlFormatting
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
		public virtual char ParameterPrefix => '@';

		/// <summary>
		/// Escapes a fragment of a LIKE pattern.
		/// </summary>
		/// <returns>The string fragment, with the special characters <c>%</c>
		/// and <c>_</c> escaped as needed. This string is not raw SQL, but rather
		/// a fragment of a LIKE pattern that should be concatenated with the rest of
		/// the LIKE pattern and sent to the database via a string parameter.</returns>
		public virtual string EscapeLikeFragment(string fragment)
		{
			return (fragment ?? throw new ArgumentNullException(nameof(fragment)))
				.Replace(@"\", @"\\")
				.Replace("%", @"\%")
				.Replace("_", @"\_");
		}

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
