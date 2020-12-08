namespace Faithlife.Data.SqlFormatting
{
	/// <summary>
	/// Delegates methods to an inner SQL syntax.
	/// </summary>
	public class DelegatingSqlSyntax : SqlSyntax
	{
		/// <summary>
		/// Creates an instance that delegates to the specified SQL syntax.
		/// </summary>
		public DelegatingSqlSyntax(SqlSyntax inner) => Inner = inner;

		/// <inheritdoc />
		public override char ParameterPrefix => Inner.ParameterPrefix;

		/// <inheritdoc />
		public override string EscapeLikeFragment(string fragment) => Inner.EscapeLikeFragment(fragment);

		/// <inheritdoc />
		public override string QuoteName(string name) => Inner.QuoteName(name);

		/// <summary>
		/// The inner SQL syntax.
		/// </summary>
		protected SqlSyntax Inner { get; }
	}
}
