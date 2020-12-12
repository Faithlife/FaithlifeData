using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Faithlife.Data.SqlFormatting
{
	/// <summary>
	/// Encapsulates parameterized SQL.
	/// </summary>
	[SuppressMessage("Naming", "CA1724", Justification = "Conflicts with rarely-used System.Data.Sql namespace.")]
	public abstract class Sql
	{
		/// <summary>
		/// An empty SQL string.
		/// </summary>
		public static readonly Sql Empty = Sql.Raw("");

		/// <summary>
		/// Concatenates SQL fragments.
		/// </summary>
		public static Sql Concat(params Sql[] sqls) =>
			new ConcatSql(sqls ?? throw new ArgumentNullException(nameof(sqls)));

		/// <summary>
		/// Concatenates SQL fragments.
		/// </summary>
		public static Sql Concat(IEnumerable<Sql> sqls) =>
			new ConcatSql(AsReadOnlyList(sqls ?? throw new ArgumentNullException(nameof(sqls))));

		/// <summary>
		/// Creates SQL from a formatted string.
		/// </summary>
		public static Sql Format(FormattableString formattableString) => new FormatSql(formattableString ?? throw new ArgumentNullException(nameof(formattableString)));

		/// <summary>
		/// Joins SQL fragments with the specified separator.
		/// </summary>
		public static Sql Join(string separator, params Sql[] sqls) =>
			new JoinSql(separator ?? throw new ArgumentNullException(nameof(separator)), sqls ?? throw new ArgumentNullException(nameof(sqls)));

		/// <summary>
		/// Joins SQL fragments with the specified separator.
		/// </summary>
		public static Sql Join(string separator, IEnumerable<Sql> sqls) =>
			new JoinSql(separator ?? throw new ArgumentNullException(nameof(separator)), AsReadOnlyList(sqls ?? throw new ArgumentNullException(nameof(sqls))));

		/// <summary>
		/// Creates SQL for an arbitrarily named parameter with the specified fragment of a LIKE pattern followed by a trailing <c>%</c>.
		/// </summary>
		/// <remarks>The default implementation escapes <c>%</c> and <c>_</c> in the prefix with <c>\</c>. Depending on the database
		/// and its settings, <c>escape '\'</c> may be needed after the parameter.</remarks>
		public static Sql LikePrefixParam(string prefix) => new LikePrefixParamSql(prefix ?? throw new ArgumentNullException(nameof(prefix)));

		/// <summary>
		/// Creates SQL for a quoted identifier.
		/// </summary>
		public static Sql Name(string identifier) => new NameSql(identifier ?? throw new ArgumentNullException(nameof(identifier)));

		/// <summary>
		/// Creates SQL for an arbitrarily named parameter with the specified value.
		/// </summary>
		public static Sql Param(object? value) => new ParamSql(value);

		/// <summary>
		/// Creates SQL from a raw string.
		/// </summary>
		public static Sql Raw(string text) => new RawSql(text ?? throw new ArgumentNullException(nameof(text)));

		/// <summary>
		/// Concatenates two SQL fragments.
		/// </summary>
		[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use Concat.")]
		public static Sql operator +(Sql a, Sql b) => new AddSql(a, b);

		internal abstract string Render(SqlContext context);

		private static IReadOnlyList<T> AsReadOnlyList<T>(IEnumerable<T> items) => (items as IReadOnlyList<T>) ?? items.ToList();

		private sealed class AddSql : Sql
		{
			public AddSql(Sql a, Sql b) => (m_a, m_b) = (a, b);
			internal override string Render(SqlContext context) => m_a.Render(context) + m_b.Render(context);
			private readonly Sql m_a;
			private readonly Sql m_b;
		}

		private sealed class FormatSql : Sql
		{
			public FormatSql(FormattableString formattableString) => m_formattableString = formattableString;
			internal override string Render(SqlContext context) => m_formattableString.ToString(new SqlFormatProvider(context));
			private readonly FormattableString m_formattableString;
		}

		private sealed class ConcatSql : Sql
		{
			public ConcatSql(IReadOnlyList<Sql> sqls) => m_sqls = sqls;
			internal override string Render(SqlContext context) => string.Concat(m_sqls.Select(x => x.Render(context)));
			private readonly IReadOnlyList<Sql> m_sqls;
		}

		private sealed class JoinSql : Sql
		{
			public JoinSql(string separator, IReadOnlyList<Sql> sqls) => (m_separator, m_sqls) = (separator, sqls);
			internal override string Render(SqlContext context) => string.Join(m_separator, m_sqls.Select(x => x.Render(context)));
			private readonly string m_separator;
			private readonly IReadOnlyList<Sql> m_sqls;
		}

		private sealed class LikePrefixParamSql : Sql
		{
			public LikePrefixParamSql(string prefix) => m_prefix = prefix;
			internal override string Render(SqlContext context) => context.RenderParam(context.Syntax.EscapeLikeFragment(m_prefix) + "%");
			private readonly string m_prefix;
		}

		private sealed class NameSql : Sql
		{
			public NameSql(string identifier) => m_identifier = identifier;
			internal override string Render(SqlContext context) => context.Syntax.QuoteName(m_identifier);
			private readonly string m_identifier;
		}

		private sealed class ParamSql : Sql
		{
			public ParamSql(object? value) => m_value = value;
			internal override string Render(SqlContext context) => context.RenderParam(m_value);
			private readonly object? m_value;
		}

		private sealed class RawSql : Sql
		{
			public RawSql(string text) => m_text = text;
			internal override string Render(SqlContext context) => m_text;
			private readonly string m_text;
		}

		private sealed class SqlFormatProvider : IFormatProvider, ICustomFormatter
		{
			public SqlFormatProvider(SqlContext context) => m_context = context;

			public object GetFormat(Type formatType) => this;

			public string Format(string? format, object? arg, IFormatProvider formatProvider)
			{
				if (format is object)
					throw new FormatException($"Format specifier '{format}' is not supported.");
				return arg is Sql sql ? sql.Render(m_context) : m_context.RenderParam(arg);
			}

			private readonly SqlContext m_context;
		}
	}
}
