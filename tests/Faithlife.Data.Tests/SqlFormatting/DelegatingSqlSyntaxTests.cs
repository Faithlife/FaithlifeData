using System.Linq;
using System.Reflection;
using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests.SqlFormatting
{
	[TestFixture]
	public class DelegatingSqlSyntaxTests
	{
		[Test]
		public void DelegateAllVirtuals()
		{
			var methods = typeof(SqlSyntax).GetMethods()
				.Concat(typeof(SqlSyntax).GetProperties().Select(x => x.GetMethod!))
				.Where(x => x.DeclaringType == typeof(SqlSyntax) && (x.IsAbstract || x.IsVirtual))
				.ToList();
			var delegated = new DelegatedSqlSyntax();
			InvokeMethods(delegated);
			var delegating = new DelegatingSqlSyntax(delegated);
			InvokeMethods(delegating);

			void InvokeMethods(SqlSyntax syntax)
			{
				foreach (var method in methods)
				{
					Invoking(() => method.Invoke(syntax, new object[method.GetParameters().Length]))
						.Should().Throw<TargetInvocationException>().WithInnerException<DelegatedException>();
				}
			}
		}

		private sealed class DelegatedSqlSyntax : SqlSyntax
		{
			public override char ParameterPrefix => throw new DelegatedException();
			public override bool UseSnakeCase => throw new DelegatedException();
			public override string EscapeLikeFragment(string fragment) => throw new DelegatedException();
			public override string QuoteName(string name) => throw new DelegatedException();
		}
	}
}
