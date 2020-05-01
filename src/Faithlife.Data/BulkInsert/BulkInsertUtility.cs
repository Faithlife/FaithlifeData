using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data.BulkInsert
{
	/// <summary>
	/// Methods for performing bulk inserts.
	/// </summary>
	public static class BulkInsertUtility
	{
		/// <summary>
		/// Efficiently inserts multiple rows, in batches as necessary.
		/// </summary>
		public static int BulkInsert(this DbConnectorCommand command, IEnumerable<DbParameters> rows, BulkInsertSettings? settings = null)
		{
			var rowCount = 0;
			foreach (var (sql, parameters) in GetBulkInsertCommands(command.Text, command.Parameters, rows, settings))
				rowCount += command.Connector.Command(sql, parameters).Execute();
			return rowCount;
		}

		/// <summary>
		/// Efficiently inserts multiple rows, in batches as necessary.
		/// </summary>
		public static async Task<int> BulkInsertAsync(this DbConnectorCommand command, IEnumerable<DbParameters> rows, BulkInsertSettings? settings = null, CancellationToken cancellationToken = default)
		{
			var rowCount = 0;
			foreach (var (sql, parameters) in GetBulkInsertCommands(command.Text, command.Parameters, rows, settings))
				rowCount += await command.Connector.Command(sql, parameters).ExecuteAsync(cancellationToken).ConfigureAwait(false);
			return rowCount;
		}

		// internal for unit testing
		internal static IEnumerable<(string Sql, DbParameters Parameters)> GetBulkInsertCommands(string sql, DbParameters commonParameters, IEnumerable<DbParameters> rows, BulkInsertSettings? settings = null)
		{
			if (rows is null)
				throw new ArgumentNullException(nameof(rows));

			var valuesClauseMatches = s_valuesClauseRegex.Matches(sql);
			if (valuesClauseMatches.Count == 0)
				throw new ArgumentException("SQL does not contain 'VALUES (' followed by ')...'.", nameof(sql));
			if (valuesClauseMatches.Count > 1)
				throw new ArgumentException("SQL contains more than one 'VALUES (' followed by ')...'.", nameof(sql));

			var valuesClauseMatch = valuesClauseMatches[0];
			var tupleMatch = valuesClauseMatch.Groups[1];
			var sqlPrefix = sql.Substring(0, tupleMatch.Index);
			var sqlSuffix = sql.Substring(valuesClauseMatch.Index + valuesClauseMatch.Length);

			var tupleParts = s_parameterRegex.Split(tupleMatch.Value);
			var tupleParameters = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);
			for (var index = 1; index < tupleParts.Length; index += 2)
			{
				var name = tupleParts[index];
				tupleParameters[name] = tupleParameters.TryGetValue(name, out var indices) ? indices.Append(index).ToArray() : new[] { index };
				name = name.Substring(1);
				tupleParameters[name] = tupleParameters.TryGetValue(name, out indices) ? indices.Append(index).ToArray() : new[] { index };
			}

			var maxParametersPerBatch = settings?.MaxParametersPerBatch ?? (settings?.MaxRecordsPerBatch is null ? c_defaultMaxParametersPerBatch : int.MaxValue);
			if (maxParametersPerBatch < 1)
				throw new ArgumentException($"{nameof(settings.MaxParametersPerBatch)} setting must be positive.");

			var maxRecordsPerBatch = settings?.MaxRecordsPerBatch ?? int.MaxValue;
			if (maxRecordsPerBatch < 1)
				throw new ArgumentException($"{nameof(settings.MaxRecordsPerBatch)} setting must be positive.");

			var batchSqls = new List<string>();
			Dictionary<string, object?>? batchParameters = null;
			var rowParts = new string[tupleParts.Length];
			string getBatchSql() => sqlPrefix + string.Join(", ", batchSqls) + sqlSuffix;

			foreach (var rowParameters in rows)
			{
				batchParameters ??= commonParameters.ToDictionary();

				var recordIndex = batchSqls.Count;
				Array.Copy(tupleParts, rowParts, tupleParts.Length);

				foreach (var rowParameter in rowParameters)
				{
					if (tupleParameters.TryGetValue(rowParameter.Name, out var indices))
					{
						foreach (var index in indices)
						{
							rowParts[index] = $"{rowParts[index]}_{recordIndex}";
							batchParameters[$"{rowParameter.Name}_{recordIndex}"] = rowParameter.Value;
						}
					}
				}

				batchSqls.Add(string.Concat(rowParts));

				if (batchSqls.Count == maxRecordsPerBatch || batchParameters.Count + tupleParts.Length / 2 > maxParametersPerBatch)
				{
					yield return (getBatchSql(), DbParameters.Create(batchParameters));
					batchSqls.Clear();
					batchParameters = null;
				}
			}

			if (batchSqls.Count != 0)
				yield return (getBatchSql(), DbParameters.Create(batchParameters!));
		}

		private const int c_defaultMaxParametersPerBatch = 999;

		private static readonly Regex s_valuesClauseRegex = new Regex(
			@"\b[vV][aA][lL][uU][eE][sS]\s*(\(.*?\))\s*\.\.\.", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.RightToLeft);

		private static readonly Regex s_parameterRegex = new Regex(@"([?@:]\w+)\b", RegexOptions.CultureInvariant);
	}
}
