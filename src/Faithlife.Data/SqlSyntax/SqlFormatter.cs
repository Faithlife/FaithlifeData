using System;
using System.Globalization;

namespace Faithlife.Data.SqlSyntax
{
	public class SqlFormatter
	{
		public static SqlFormatter Default = new SqlFormatter();

		public string Format(FormattableString formattable) =>
			formattable.ToString(new SqlFormatProvider(this));

		public virtual string RenderString(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			return $"'{value.Replace("'", "''")}'";
		}

		public virtual string RenderBoolean(bool value) => value ? "1" : "0";

		public string RenderLiteral(object value)
		{
			return TryRenderLiteral(value, out string text) ? text : throw new ArgumentException($"Type cannot be rendered as a literal: {value.GetType().FullName}", nameof(value));
		}

		public bool TryRenderLiteral(object value, out string text)
		{
			text = null;

			if (value == null)
				text = "NULL";
			else if (value is string stringValue)
				text = RenderString(stringValue);
			else if (value is bool boolValue)
				text = RenderBoolean(boolValue);
			else if (value is IFormattable formattable && (value is int || value is long || value is short || value is float || value is double || value is decimal))
				text = formattable.ToString(null, CultureInfo.InvariantCulture);

			return text != null;
		}

		private sealed class SqlFormatProvider : IFormatProvider, ICustomFormatter
		{
			public SqlFormatProvider(SqlFormatter sqlFormatter)
			{
				m_sqlFormatter = sqlFormatter;
			}

			public object GetFormat(Type formatType) => this;

			public string Format(string format, object arg, IFormatProvider formatProvider)
			{
				if (format == "raw")
				{
					if (arg is string stringValue)
						return stringValue;
					if (arg == null)
						return "";

					throw new FormatException("Format 'raw' can only be used with strings.");
				}
				else if (format == "literal")
				{
					if (m_sqlFormatter.TryRenderLiteral(arg, out string text))
						return text;

					throw new FormatException($"Format 'literal' cannot be used with type: {arg.GetType().FullName}");
				}
				else if (format != null)
				{
					throw new FormatException($"Format '{format}' is not supported.");
				}
				else
				{
					throw new FormatException($"Argument of type '{arg?.GetType().FullName}' requires a format, e.g. {{value:literal}}.");
				}
			}

			private readonly SqlFormatter m_sqlFormatter;
		}
	}
}
