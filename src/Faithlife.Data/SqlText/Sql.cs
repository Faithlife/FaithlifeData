using System;
using System.Diagnostics.CodeAnalysis;

namespace Faithlife.Data.SqlText
{
	/// <summary>
	/// Encapsulates parameterized SQL.
	/// </summary>
	[SuppressMessage("Naming", "CA1724", Justification = "Conflicts with rarely-used System.Data.Sql namespace.")]
	public abstract class Sql
	{
		/// <summary>
		/// Creates SQL from a raw string.
		/// </summary>
		public static Sql Raw(string text) => new RawSql(text);

		/// <summary>
		/// Creates SQL from a formatted string.
		/// </summary>
		public static Sql Format(FormattableString formattableString) => new FormattableSql(formattableString);

		internal abstract string Render(SqlContext context);

		private sealed class RawSql : Sql
		{
			public RawSql(string text) => m_text = text;
			internal override string Render(SqlContext context) => m_text;
			private readonly string m_text;
		}

		private sealed class FormattableSql : Sql
		{
			public FormattableSql(FormattableString formattableString) => m_formattableString = formattableString;
			internal override string Render(SqlContext context) => m_formattableString.ToString(new SqlFormatProvider(context));
			private readonly FormattableString m_formattableString;
		}

		private sealed class SqlFormatProvider : IFormatProvider, ICustomFormatter
		{
			public SqlFormatProvider(SqlContext context) => m_context = context;

			public object GetFormat(Type formatType) => this;

			public string Format(string? format, object? arg, IFormatProvider formatProvider)
			{
				switch (format)
				{
					case "raw":
						if (arg is string stringValue)
							return stringValue;
						throw new FormatException("Format 'raw' can only be used with strings.");

					default:
						if (format is object)
							throw new FormatException($"Format '{format}' is not supported.");
						if (arg is Sql sql)
							return sql.Render(m_context);
						throw new FormatException("Argument requires a format, e.g. {value:raw}.");
				}
			}

			private readonly SqlContext m_context;
		}
	}
}
