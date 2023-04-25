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
	public static readonly Sql Empty = Sql.Raw("");

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

	private sealed class ColumnParamsSql : Sql
	{
		public ColumnParamsSql(object dto, Func<string, bool>? filter = null) => (m_dto, m_filter) = (dto, filter);

		internal override string Render(SqlContext context)
		{
			var type = m_dto.GetType();
			var properties = DtoInfo.GetInfo(type).Properties;
			if (properties.Count == 0)
				throw new InvalidOperationException($"The specified type has no columns: {type.FullName}");

			IEnumerable<IDtoProperty> filteredProperties = properties;
			if (m_filter is not null)
				filteredProperties = filteredProperties.Where(x => m_filter(x.Name));

			var text = string.Join(", ", filteredProperties.Select(x => context.RenderParam(x.GetValue(m_dto))));
			if (text.Length == 0)
				throw new InvalidOperationException($"The specified type has no remaining columns: {type.FullName}");
			return text;
		}

		private readonly object m_dto;
		private readonly Func<string, bool>? m_filter;
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

		public object GetFormat(Type? formatType) => this;

		public string Format(string? format, object? arg, IFormatProvider? formatProvider)
		{
			if (format is not null)
				throw new FormatException($"Format specifier '{format}' is not supported.");
			return arg is Sql sql ? sql.Render(m_context) : m_context.RenderParam(arg);
		}

		private readonly SqlContext m_context;
	}
}
