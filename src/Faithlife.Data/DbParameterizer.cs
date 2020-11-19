using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Faithlife.Utility;
using static System.FormattableString;

namespace Faithlife.Data
{
	/// <summary>
	/// Converts <c>FormattableString</c> into text and <c>DbParameters</c>.
	/// </summary>
	public static class DbParameterizer
	{
		/// <summary>
		/// Converts <c>FormattableString</c> into text and <c>DbParameters</c>.
		/// </summary>
		public static (string Text, DbParameters Parameters) Sql(FormattableString sql)
		{
			if (sql is null)
				throw new ArgumentNullException(nameof(sql));
			var argumentCount = sql.ArgumentCount;
			if (argumentCount > c_maxParameterCount)
				throw new ArgumentException("Too many parameters", nameof(sql));
			if (argumentCount == 0)
				return (sql.Format, DbParameters.Empty);
			var text = string.Format(CultureInfo.InvariantCulture, sql.Format, s_parameterPlaceholders[argumentCount - 1].Value);
			var parameters = DbParameters.Create(s_parameterNames.Take(sql.ArgumentCount).Zip(sql.GetArguments()));
			return (text, parameters);
		}

		private const int c_maxParameterCount = 100;
		private static readonly IReadOnlyList<string> s_parameterNames = Enumerable.Range(0, c_maxParameterCount).Select(i => Invariant($"p{i}")).ToArray();
		private static readonly IReadOnlyList<string> s_allParameterPlaceholders = s_parameterNames.Select(p => Invariant($"@{p}")).ToArray();
		private static readonly IReadOnlyList<Lazy<object[]>> s_parameterPlaceholders = Enumerable.Range(0, c_maxParameterCount).Select(i => new Lazy<object[]>(() =>
			{
				var arr = new object[i + 1];
				for (var j = 0; j < arr.Length; j++)
					arr[j] = s_allParameterPlaceholders[j];
				return arr;
			}, LazyThreadSafetyMode.PublicationOnly)).ToArray();
	}
}
