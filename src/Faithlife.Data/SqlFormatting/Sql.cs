using System;
using System.Diagnostics.CodeAnalysis;

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
		/// Creates SQL for an arbitrarily named parameter with the specified fragment of a LIKE pattern followed by a trailing <c>%</c>.
		/// </summary>
		/// <remarks>The default implementation escapes <c>%</c> and <c>_</c> in the prefix with <c>\</c>. Depending on the database
		/// and its settings, <c>escape '\'</c> may be needed after the parameter.</remarks>
		public static Sql LikePrefixParam(string prefix) => new LikePrefixParamSql(prefix ?? throw new ArgumentNullException(nameof(prefix)));

		/// <summary>
		/// Creates SQL for an arbitrarily named parameter with the specified value.
		/// </summary>
		public static Sql Param(object? value) => new ParamSql(value);

		/// <summary>
		/// Creates SQL from a raw string.
		/// </summary>
		public static Sql Raw(string text) => new RawSql(text ?? throw new ArgumentNullException(nameof(text)));

		internal abstract string Render(SqlContext context);

		private sealed class FormatSql : Sql
		{
			public FormatSql(FormattableString formattableString) => m_formattableString = formattableString;
			internal override string Render(SqlContext context) => m_formattableString.ToString(new SqlFormatProvider(context));
			private readonly FormattableString m_formattableString;
		}

		private sealed class LikePrefixParamSql : Sql
		{
			public LikePrefixParamSql(string prefix) => m_prefix = prefix;
			internal override string Render(SqlContext context) => context.RenderParam(context.Syntax.EscapeLikeFragment(m_prefix) + "%");
			private readonly string m_prefix;
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
