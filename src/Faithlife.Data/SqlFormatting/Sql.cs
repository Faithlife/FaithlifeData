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
		/// Creates SQL for an arbitrarily named parameter with the specified value.
		/// </summary>
		public static Sql Param(object? value) => new ParamSql(value);

		/// <summary>
		/// Creates SQL from a raw string.
		/// </summary>
		public static Sql Raw(string text) => new RawSql(text ?? throw new ArgumentNullException(nameof(text)));

		internal abstract string Render(SqlContext context);

		private static IReadOnlyList<T> AsReadOnlyList<T>(IEnumerable<T> items) => (items as IReadOnlyList<T>) ?? items.ToList();

		private sealed class FormatSql : Sql
		{
			public FormatSql(FormattableString formattableString) => m_formattableString = formattableString;
			internal override string Render(SqlContext context) => m_formattableString.ToString(new SqlFormatProvider(context));
			private readonly FormattableString m_formattableString;
		}

		private sealed class JoinSql : Sql
		{
			public JoinSql(string separator, IReadOnlyList<Sql> sqls) => (m_separator, m_sqls) = (separator, sqls);
			internal override string Render(SqlContext context) => string.Join(m_separator, m_sqls.Select(x => x.Render(context)));
			private readonly string m_separator;
			private readonly IReadOnlyList<Sql> m_sqls;
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
				switch (format)
				{
					case "param":
						return m_context.RenderParam(arg);

					case "raw":
						if (arg is string stringValue)
							return stringValue;
						throw new FormatException("Format 'raw' can only be used with strings.");

					default:
						if (format is object)
							throw new FormatException($"Format '{format}' is not supported.");
						return arg is Sql sql ? sql.Render(m_context) : m_context.RenderParam(arg);
				}
			}

			private readonly SqlContext m_context;
		}
	}
}
