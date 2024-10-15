using static System.FormattableString;

namespace Faithlife.Data.SqlFormatting;

internal sealed class SqlContext
{
	public SqlContext(SqlSyntax syntax)
	{
		Syntax = syntax;
	}

	public SqlSyntax Syntax { get; }

	public DbParameters Parameters => m_parameters is null ? DbParameters.Empty : DbParameters.Create(m_parameters);

	public string RenderParam(object? key, object? value)
	{
		if (key is not null && m_renderedParams is not null && m_renderedParams.TryGetValue(key, out var rendered))
			return rendered;

		m_parameters ??= [];
		var name = Invariant($"fdp{m_parameters.Count}");
		m_parameters.Add((name, value));
		rendered = Syntax.ParameterPrefix + name;

		if (key is not null)
		{
			m_renderedParams ??= new();
			m_renderedParams.Add(key, rendered);
		}

		return rendered;
	}

	private List<(string Name, object? Value)>? m_parameters;
	private Dictionary<object, string>? m_renderedParams;
}
