using static System.FormattableString;

namespace Faithlife.Data.SqlFormatting
{
	internal sealed class SqlContext
	{
		public SqlContext(SqlSyntax syntax)
		{
			Syntax = syntax;
		}

		public SqlSyntax Syntax { get; }

		public DbParameters Parameters => m_parameters is null ? DbParameters.Empty : DbParameters.Create(m_parameters);

		public string RenderParam(object? value)
		{
			m_parameters ??= new List<(string Name, object? Value)>();
			var name = Invariant($"fdp{m_parameters.Count}");
			m_parameters.Add((name, value));
			return Syntax.ParameterPrefix + name;
		}

		private List<(string Name, object? Value)>? m_parameters;
	}
}
