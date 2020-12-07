using System;

namespace Faithlife.Data.SqlFormatting
{
	/// <summary>
	/// Utility methods for implementing SQL syntaxes.
	/// </summary>
	public static class SqlSyntaxUtility
	{
		/// <summary>
		/// Escapes a fragment of a LIKE pattern with the specified escape character.
		/// </summary>
		/// <returns>The string fragment, with the special characters <c>%</c>
		/// and <c>_</c> escaped as needed. This string is not raw SQL, but rather
		/// a fragment of a LIKE pattern that should be concatenated with the rest of
		/// the LIKE pattern and passed to the SQL server via a string parameter.</returns>
		public static string EscapeLikeFragment(string fragment, char escapeChar)
		{
			var escape = escapeChar.ToString();
			return fragment
				.Replace(escape, escape + escape)
				.Replace("%", escape + "%")
				.Replace("_", escape + "_");
		}
	}
}
