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
		/// <remarks>The default syntax does not support quoted identifiers, since the syntax
		/// is highly dependent on the type of database and its settings.</remarks>
		public static SqlSyntax Default { get; } = new DefaultSqlSyntax();

		/// <summary>
		/// The syntax for MySQL.
		/// </summary>
		public static SqlSyntax MySql { get; } = new SpecificSqlSyntax(nameQuote: '`');

		/// <summary>
		/// The syntax for PostgreSQL.
		/// </summary>
		public static SqlSyntax Postgres { get; } = new SpecificSqlSyntax(nameQuote: '"');

		/// <summary>
		/// The syntax for Microsoft SQL Server.
		/// </summary>
		public static SqlSyntax SqlServer { get; } = new SpecificSqlSyntax(nameQuote: '[');

		/// <summary>
		/// The syntax for SQLite.
		/// </summary>
		public static SqlSyntax Sqlite { get; } = new SpecificSqlSyntax(nameQuote: '"');

		/// <summary>
		/// The prefix for named parameters.
		/// </summary>
		public virtual char ParameterPrefix => '@';

		/// <summary>
		/// Escapes a fragment of a LIKE pattern.
		/// </summary>
		/// <returns>The string fragment, with wildcard characters such as <c>%</c>
		/// and <c>_</c> escaped as needed. This string is not raw SQL, but rather
		/// a fragment of a LIKE pattern that should be concatenated with the rest of
		/// the LIKE pattern and sent to the database via a string parameter.</returns>
		public virtual string EscapeLikeFragment(string fragment)
		{
			const string escapeString = @"\";
			return (fragment ?? throw new ArgumentNullException(nameof(fragment)))
				.Replace(escapeString, escapeString + escapeString)
				.Replace("%", escapeString + "%")
				.Replace("_", escapeString + "_");
		}

		/// <summary>
		/// Quotes the specified identifier so that it can be used as a schema/table/column name
		/// even if it matches a keyword or has special characters.
		/// </summary>
		public virtual string QuoteName(string name) =>
			throw new InvalidOperationException("The default SqlSyntax does not support quoted identifiers. Use a SqlSyntax that matches your database.");

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

		private sealed class SpecificSqlSyntax : SqlSyntax
		{
			public SpecificSqlSyntax(char nameQuote)
			{
				(m_nameQuoteStart, m_nameQuoteEnd, m_nameQuoteEndEscape) = nameQuote switch
				{
					'"' => ("\"", "\"", "\"\""),
					'`' => ("`", "`", "``"),
					'[' => ("[", "]", "]]"),
					_ => throw new ArgumentOutOfRangeException(nameof(nameQuote)),
				};
			}

			public override string QuoteName(string name) =>
				m_nameQuoteStart + name.Replace(m_nameQuoteEnd, m_nameQuoteEndEscape) + m_nameQuoteEnd;

			private readonly string m_nameQuoteStart;
			private readonly string m_nameQuoteEnd;
			private readonly string m_nameQuoteEndEscape;
		}
	}
}
