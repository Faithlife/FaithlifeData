using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Faithlife.Data.SqlFormatting;

/// <summary>
/// Encapsulates parameterized SQL.
/// </summary>
[SuppressMessage("Naming", "CA1724", Justification = "Conflicts with rarely-used System.Data.Sql namespace.")]
public abstract class Sql
{
	/// <summary>
	/// An empty SQL string.
	/// </summary>
	public static readonly Sql Empty = Raw("");

	/// <summary>
	/// Joins the specified SQL fragments with the AND operator.
	/// </summary>
	public static Sql And(params Sql[] sqls) => And(sqls.AsEnumerable());

	/// <summary>
	/// Joins the specified SQL fragments with the AND operator.
	/// </summary>
	public static Sql And(IEnumerable<Sql> sqls) => new BinaryOperatorSql(" and ", " AND ", AsReadOnlyList(sqls));

	/// <summary>
	/// Joins the specified SQL fragments with newlines.
	/// </summary>
	public static Sql Clauses(params Sql[] sqls) => Clauses(sqls.AsEnumerable());

	/// <summary>
	/// Joins the specified SQL fragments with newlines.
	/// </summary>
	public static Sql Clauses(IEnumerable<Sql> sqls) => Join("\n", sqls);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type.
	/// </summary>
	public static Sql ColumnNames<T>() => new ColumnNamesSql(typeof(T));

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type.
	/// </summary>
	public static Sql ColumnNames(Type type) => new ColumnNamesSql(type ?? throw new ArgumentNullException(nameof(type)));

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type.
	/// </summary>
	/// <remarks>This overload is used with SELECT statements when the table name (or alias)
	/// needs to be specified with each column name. If a tuple of DTOs is specified, a NULL column
	/// will separate the DTOs.</remarks>
	public static Sql ColumnNames<T>(string tableName) => new ColumnNamesSql(typeof(T), tableName);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type.
	/// </summary>
	/// <remarks>This overload is used with SELECT statements when the table name (or alias)
	/// needs to be specified with each column name. If a tuple of DTOs is specified, a NULL column
	/// will separate the DTOs.</remarks>
	public static Sql ColumnNames(Type type, string tableName) => new ColumnNamesSql(type ?? throw new ArgumentNullException(nameof(type)), tableName);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type.
	/// </summary>
	/// <remarks>This overload is used with SELECT statements when the table name (or alias)
	/// needs to be specified with each column name. If a tuple of DTOs is specified, a NULL column
	/// will separate the DTOs.</remarks>
	public static Sql ColumnNames<T>(params string[] tableNames) => new ColumnNamesSql(typeof(T), tableNames);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type.
	/// </summary>
	/// <remarks>This overload is used with SELECT statements when the table name (or alias)
	/// needs to be specified with each column name. If a tuple of DTOs is specified, a NULL column
	/// will separate the DTOs.</remarks>
	public static Sql ColumnNames(Type type, params string[] tableNames) => new ColumnNamesSql(type ?? throw new ArgumentNullException(nameof(type)), tableNames);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type
	/// for the properties whose names match the specified filter.
	/// </summary>
	public static Sql ColumnNamesWhere<T>(Func<string, bool> filter) => new ColumnNamesSql(typeof(T), filter);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type
	/// for the properties whose names match the specified filter.
	/// </summary>
	public static Sql ColumnNamesWhere(Type type, Func<string, bool> filter) => new ColumnNamesSql(type ?? throw new ArgumentNullException(nameof(type)), filter);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type
	/// for the properties whose names match the specified filter.
	/// </summary>
	/// <remarks>This overload is used with SELECT statements when the table name (or alias)
	/// needs to be specified with each column name. If a tuple of DTOs is specified, a NULL column
	/// will separate the DTOs.</remarks>
	public static Sql ColumnNamesWhere<T>(Func<string, bool> filter, string tableName) => new ColumnNamesSql(typeof(T), tableName, filter);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type
	/// for the properties whose names match the specified filter.
	/// </summary>
	/// <remarks>This overload is used with SELECT statements when the table name (or alias)
	/// needs to be specified with each column name. If a tuple of DTOs is specified, a NULL column
	/// will separate the DTOs.</remarks>
	public static Sql ColumnNamesWhere(Type type, Func<string, bool> filter, string tableName) => new ColumnNamesSql(type ?? throw new ArgumentNullException(nameof(type)), tableName, filter);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type
	/// for the properties whose names match the specified filter.
	/// </summary>
	/// <remarks>This overload is used with SELECT statements when the table name (or alias)
	/// needs to be specified with each column name. If a tuple of DTOs is specified, a NULL column
	/// will separate the DTOs.</remarks>
	public static Sql ColumnNamesWhere<T>(Func<string, bool> filter, params string[] tableNames) => new ColumnNamesSql(typeof(T), tableNames, filter);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type
	/// for the properties whose names match the specified filter.
	/// </summary>
	/// <remarks>This overload is used with SELECT statements when the table name (or alias)
	/// needs to be specified with each column name. If a tuple of DTOs is specified, a NULL column
	/// will separate the DTOs.</remarks>
	public static Sql ColumnNamesWhere(Type type, Func<string, bool> filter, params string[] tableNames) => new ColumnNamesSql(type ?? throw new ArgumentNullException(nameof(type)), tableNames, filter);

	/// <summary>
	/// Returns a comma-delimited list of arbitrarily-named parameters for the column values of the specified DTO.
	/// </summary>
	public static Sql ColumnParams(object dto) => new ColumnParamsSql(dto ?? throw new ArgumentNullException(nameof(dto)));

	/// <summary>
	/// Returns a comma-delimited list of arbitrarily-named parameters for the column values of the specified DTO
	/// for the properties whose names match the specified filter.
	/// </summary>
	public static Sql ColumnParamsWhere(object dto, Func<string, bool> filter) =>
		new ColumnParamsSql(dto ?? throw new ArgumentNullException(nameof(dto)), filter ?? throw new ArgumentNullException(nameof(filter)));

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
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNames<T>() => DtoParamNames(typeof(T));

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNames(Type type) => new DtoParamNamesSql(type ?? throw new ArgumentNullException(nameof(type)));

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNames<T>(string name) => DtoParamNames(typeof(T), name);

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNames(Type type, string name) => new DtoParamNamesSql(type ?? throw new ArgumentNullException(nameof(type)), name ?? throw new ArgumentNullException(nameof(name)));

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNames<T>(Func<string, string> name) => DtoParamNames(typeof(T), name);

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNames(Type type, Func<string, string> name) => new DtoParamNamesSql(type ?? throw new ArgumentNullException(nameof(type)), name ?? throw new ArgumentNullException(nameof(name)));

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO
	/// whose names match the specified filter.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNamesWhere<T>(Func<string, bool> filter) => DtoParamNamesWhere(typeof(T), filter);

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO
	/// whose names match the specified filter.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNamesWhere(Type type, Func<string, bool> filter) =>
		new DtoParamNamesSql(type ?? throw new ArgumentNullException(nameof(type)), filter ?? throw new ArgumentNullException(nameof(filter)));

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO
	/// whose names match the specified filter.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNamesWhere<T>(string name, Func<string, bool> filter) => DtoParamNamesWhere(typeof(T), name, filter);

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO
	/// whose names match the specified filter.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNamesWhere(Type type, string name, Func<string, bool> filter) =>
		new DtoParamNamesSql(type ?? throw new ArgumentNullException(nameof(type)), name ?? throw new ArgumentNullException(nameof(name)), filter ?? throw new ArgumentNullException(nameof(filter)));

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO
	/// whose names match the specified filter.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNamesWhere<T>(Func<string, string> name, Func<string, bool> filter) => DtoParamNamesWhere(typeof(T), name, filter);

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO
	/// whose names match the specified filter.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static Sql DtoParamNamesWhere(Type type, Func<string, string> name, Func<string, bool> filter) =>
		new DtoParamNamesSql(type ?? throw new ArgumentNullException(nameof(type)), name ?? throw new ArgumentNullException(nameof(name)), filter ?? throw new ArgumentNullException(nameof(filter)));

	/// <summary>
	/// Creates SQL from a formatted string.
	/// </summary>
	public static Sql Format(FormattableString formattableString) => new FormatSql(formattableString ?? throw new ArgumentNullException(nameof(formattableString)));

	/// <summary>
	/// Creates SQL for a GROUP BY clause. If the SQL is empty, the GROUP BY clause is omitted.
	/// </summary>
	public static Sql GroupBy(Sql sql) => new OptionalClauseSql("group by ", "GROUP BY ", sql);

	/// <summary>
	/// Creates SQL for a HAVING clause. If the SQL is empty, the HAVING clause is omitted.
	/// </summary>
	public static Sql Having(Sql sql) => new OptionalClauseSql("having ", "HAVING ", sql);

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
	/// Creates SQL for an arbitrarily-named parameter with the specified fragment of a LIKE pattern followed by a trailing <c>%</c>.
	/// </summary>
	/// <remarks>The default implementation escapes <c>%</c> and <c>_</c> in the prefix with <c>\</c>. Depending on the database
	/// and its settings, <c>escape '\'</c> may be needed after the parameter.</remarks>
	public static Sql LikePrefixParam(string prefix) => new LikePrefixParamSql(prefix ?? throw new ArgumentNullException(nameof(prefix)));

	/// <summary>
	/// Creates SQL for a comma-delimited list of SQL fragments.
	/// </summary>
	public static Sql List(params Sql[] sqls) => List(sqls.AsEnumerable());

	/// <summary>
	/// Creates SQL for a comma-delimited list of SQL fragments.
	/// </summary>
	public static Sql List(IEnumerable<Sql> sqls) => Join(", ", sqls);

	/// <summary>
	/// Creates SQL for a quoted identifier.
	/// </summary>
	public static Sql Name(string identifier) => new NameSql(identifier ?? throw new ArgumentNullException(nameof(identifier)));

	/// <summary>
	/// Joins the specified SQL fragments with the OR operator.
	/// </summary>
	public static Sql Or(params Sql[] sqls) => Or(sqls.AsEnumerable());

	/// <summary>
	/// Joins the specified SQL fragments with the OR operator.
	/// </summary>
	public static Sql Or(IEnumerable<Sql> sqls) => new BinaryOperatorSql(" or ", " OR ", AsReadOnlyList(sqls));

	/// <summary>
	/// Creates SQL for an ORDER BY clause. If the SQL is empty, the ORDER BY clause is omitted.
	/// </summary>
	public static Sql OrderBy(Sql sql) => new OptionalClauseSql("order by ", "ORDER BY ", sql);

	/// <summary>
	/// Creates SQL for an arbitrarily-named parameter with the specified value.
	/// </summary>
	public static Sql Param(object? value)
	{
		if (value is Sql)
			throw new ArgumentException("Param may not be used with Sql instances.", nameof(value));
		return new ParamSql(value);
	}

	/// <summary>
	/// Creates SQL for a comma-delimted list of arbitrarily-named parameters with the specified values.
	/// </summary>
	public static Sql ParamList(IEnumerable values) => ParamList(values.Cast<object?>());

	/// <summary>
	/// Creates SQL for a comma-delimted list of arbitrarily-named parameters with the specified values.
	/// </summary>
	public static Sql ParamList(IEnumerable<object?> values) => List(values.Select(Param));

	/// <summary>
	/// Creates SQL for a comma-delimted list of arbitrarily-named parameters with the specified values, surrounded by parentheses.
	/// </summary>
	public static Sql ParamTuple(IEnumerable values) => ParamTuple(values.Cast<object?>());

	/// <summary>
	/// Creates SQL for a comma-delimted list of arbitrarily-named parameters with the specified values, surrounded by parentheses.
	/// </summary>
	public static Sql ParamTuple(IEnumerable<object?> values) => Tuple(values.Select(Param));

	/// <summary>
	/// Creates SQL from a raw string.
	/// </summary>
	public static Sql Raw(string text) => new RawSql(text ?? throw new ArgumentNullException(nameof(text)));

	/// <summary>
	/// Creates SQL for a comma-delimited list of SQL fragments, surrounded by parentheses.
	/// </summary>
	public static Sql Tuple(params Sql[] sqls) => Tuple(sqls.AsEnumerable());

	/// <summary>
	/// Creates SQL for a comma-delimited list of SQL fragments, surrounded by parentheses.
	/// </summary>
	public static Sql Tuple(IEnumerable<Sql> sqls) => Format($"({List(sqls)})");

	/// <summary>
	/// Creates SQL for a WHERE clause. If the SQL is empty, the WHERE clause is omitted.
	/// </summary>
	public static Sql Where(Sql sql) => new OptionalClauseSql("where ", "WHERE ", sql);

	/// <summary>
	/// Concatenates two SQL fragments.
	/// </summary>
	[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use Concat.")]
	public static Sql operator +(Sql a, Sql b) => new AddSql(a, b);

	internal abstract string Render(SqlContext context);

	private static IReadOnlyList<T> AsReadOnlyList<T>(IEnumerable<T> items) => (items as IReadOnlyList<T>) ?? items.ToList();

	private sealed class AddSql(Sql a, Sql b) : Sql
	{
		internal override string Render(SqlContext context) => a.Render(context) + b.Render(context);
	}

	private sealed class BinaryOperatorSql(string lowercase, string uppercase, IReadOnlyList<Sql> sqls) : Sql
	{
		private bool HasMultipleSqls => sqls.Count > 1;

		internal override string Render(SqlContext context)
		{
			var rawSqls = sqls
				.Select(x => (RawSql: x.Render(context), NeedsParens: x is BinaryOperatorSql { HasMultipleSqls: true }))
				.Where(x => x.RawSql.Length != 0)
				.Select(x => x.NeedsParens ? $"({x.RawSql})" : x.RawSql)
				.ToList();
			return string.Join(context.Syntax.UseLowercaseKeywords ? lowercase : uppercase, rawSqls);
		}
	}

	private sealed class ColumnNamesSql : Sql
	{
		public ColumnNamesSql(Type type, Func<string, bool>? filter = null) => (m_type, m_filter) = (type, filter);
		public ColumnNamesSql(Type type, string? tableName, Func<string, bool>? filter = null) => (m_type, m_tableName, m_filter) = (type, tableName, filter);
		public ColumnNamesSql(Type type, string[] tableNames, Func<string, bool>? filter = null) => (m_type, m_tableNames, m_filter) = (type, tableNames, filter);

		internal override string Render(SqlContext context)
		{
			if (TupleInfo.IsTupleType(m_type))
				return string.Join(", NULL, ", TupleInfo.GetInfo(m_type).ItemTypes.Select((x, i) => RenderDto(x, i, context)));

			return RenderDto(m_type, 0, context);
		}

		private string GetTableName(int index) =>
			(m_tableNames is not null ? m_tableNames.ElementAtOrDefault(index) : index == 0 ? m_tableName : null) ?? "";

		private string RenderDto(Type type, int index, SqlContext context)
		{
			var properties = DtoInfo.GetInfo(type).Properties;
			if (properties.Count == 0)
				throw new InvalidOperationException($"The specified type has no columns: {type.FullName}");

			var dbInfo = DbValueTypeInfo.GetInfo(type);
			var syntax = context.Syntax;
			var tableName = GetTableName(index);
			var tablePrefix = tableName.Length == 0 ? "" : syntax.QuoteName(tableName) + ".";
			var useSnakeCase = syntax.UseSnakeCase;

			IEnumerable<IDtoProperty> filteredProperties = properties;
			if (m_filter is not null)
				filteredProperties = filteredProperties.Where(x => m_filter(x.Name));

			var text = string.Join(", ",
				filteredProperties.Select(x => tablePrefix + syntax.QuoteName(
					dbInfo.GetColumnAttributeName(x.Name) ??
					(useSnakeCase ? s_snakeCaseCache.GetOrAdd(x.Name, ToSnakeCase) : x.Name))));
			if (text.Length == 0)
				throw new InvalidOperationException($"The specified type has no remaining columns: {type.FullName}");
			return text;
		}

		private static string ToSnakeCase(string value) => string.Join("_", s_word.Matches(value).Cast<Match>().Select(x => x.Value.ToLowerInvariant()));

		private static readonly Regex s_word = new Regex("[A-Z]([A-Z]*(?![a-z])|[a-z]*)|[a-z]+|[0-9]+", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		private static readonly ConcurrentDictionary<string, string> s_snakeCaseCache = new();

		private readonly Type m_type;
		private readonly string? m_tableName;
		private readonly string[]? m_tableNames;
		private readonly Func<string, bool>? m_filter;
	}

	private sealed class ColumnParamsSql(object dto, Func<string, bool>? filter = null) : Sql
	{
		internal override string Render(SqlContext context)
		{
			var type = dto.GetType();
			var properties = DtoInfo.GetInfo(type).Properties;
			if (properties.Count == 0)
				throw new InvalidOperationException($"The specified type has no columns: {type.FullName}");

			IEnumerable<IDtoProperty> filteredProperties = properties;
			if (filter is not null)
				filteredProperties = filteredProperties.Where(x => filter(x.Name));

			var text = string.Join(", ", filteredProperties.Select(x => context.RenderParam(key: null, value: x.GetValue(dto))));
			if (text.Length == 0)
				throw new InvalidOperationException($"The specified type has no remaining columns: {type.FullName}");
			return text;
		}
	}

	private sealed class DtoParamNamesSql : Sql
	{
		public DtoParamNamesSql(Type type, Func<string, bool>? filter = null) => (m_type, m_filter) = (type, filter);
		public DtoParamNamesSql(Type type, string name, Func<string, bool>? filter = null) => (m_type, m_name, m_filter) = (type, name, filter);
		public DtoParamNamesSql(Type type, Func<string, string> name, Func<string, bool>? filter = null) => (m_type, m_getName, m_filter) = (type, name, filter);

		internal override string Render(SqlContext context)
		{
			var properties = DtoInfo.GetInfo(m_type).Properties;
			if (properties.Count == 0)
				throw new InvalidOperationException($"The specified type has no columns: {m_type.FullName}");

			IEnumerable<IDtoProperty> filteredProperties = properties;
			if (m_filter is not null)
				filteredProperties = filteredProperties.Where(x => m_filter(x.Name));

			var text = string.Join(", ", filteredProperties.Select(x => context.Syntax.ParameterPrefix + GetName(x.Name)));
			if (text.Length == 0)
				throw new InvalidOperationException($"The specified type has no remaining columns: {m_type.FullName}");
			return text;
		}

		private string GetName(string name) =>
			m_name is not null ? $"{m_name}_{name}" : m_getName is not null ? m_getName(name) : name;

		private readonly Type m_type;
		private readonly Func<string, bool>? m_filter;
		private readonly string? m_name;
		private readonly Func<string, string>? m_getName;
	}

	private sealed class FormatSql(FormattableString formattableString) : Sql
	{
		internal override string Render(SqlContext context) => formattableString.ToString(new SqlFormatProvider(context));
	}

	private sealed class ConcatSql(IReadOnlyList<Sql> sqls) : Sql
	{
		internal override string Render(SqlContext context) => string.Concat(sqls.Select(x => x.Render(context)));
	}

	private sealed class JoinSql(string separator, IReadOnlyList<Sql> sqls) : Sql
	{
		internal override string Render(SqlContext context) => string.Join(separator, sqls.Select(x => x.Render(context)).Where(x => x.Length != 0));
	}

	private sealed class LikePrefixParamSql(string prefix) : Sql
	{
		internal override string Render(SqlContext context) => context.RenderParam(key: this, value: context.Syntax.EscapeLikeFragment(prefix) + "%");
	}

	private sealed class NameSql(string identifier) : Sql
	{
		internal override string Render(SqlContext context) => context.Syntax.QuoteName(identifier);
	}

	private sealed class OptionalClauseSql(string lowercase, string uppercase, Sql sql) : Sql
	{
		internal override string Render(SqlContext context)
		{
			var rawSql = sql.Render(context);
			return rawSql.Length == 0 ? "" : (context.Syntax.UseLowercaseKeywords ? lowercase : uppercase) + rawSql;
		}
	}

	private sealed class ParamSql(object? value) : Sql
	{
		internal override string Render(SqlContext context) => context.RenderParam(key: this, value: value);
	}

	private sealed class RawSql(string text) : Sql
	{
		internal override string Render(SqlContext context) => text;
	}

	private sealed class SqlFormatProvider(SqlContext context) : IFormatProvider, ICustomFormatter
	{
		public object GetFormat(Type? formatType) => this;

		public string Format(string? format, object? arg, IFormatProvider? formatProvider)
		{
			if (format is not null)
				throw new FormatException($"Format specifier '{format}' is not supported.");
			return arg is Sql sql ? sql.Render(context) : context.RenderParam(key: null, value: arg);
		}
	}
}
