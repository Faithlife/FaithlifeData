using System.Data;
using System.Reflection;
using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests;

[TestFixture]
public class DelegatingDbConnectorTests
{
	[Test]
	public void DelegateAllVirtuals()
	{
		var methods = typeof(DbConnector).GetMethods()
			.Concat(typeof(DbConnector).GetProperties().Select(x => x.GetMethod!))
			.Where(x => x.DeclaringType == typeof(DbConnector) && (x.IsAbstract || x.IsVirtual))
			.ToList();
		var delegated = new DelegatedDbConnector();
		InvokeMethods(delegated);
		var delegating = new DelegatingDbConnector(delegated);
		InvokeMethods(delegating);

		void InvokeMethods(DbConnector connector)
		{
			foreach (var method in methods)
			{
				Invoking(() => method.Invoke(connector, new object[method.GetParameters().Length]))
					.Should().Throw<TargetInvocationException>(method.Name).WithInnerException<DelegatedException>(method.Name);
			}
		}
	}

	private sealed class DelegatedDbConnector : DbConnector
	{
		public override IDbConnection Connection => throw new DelegatedException();

		public override IDbTransaction? Transaction => throw new DelegatedException();

		public override SqlSyntax SqlSyntax => throw new DelegatedException();

		public override ValueTask<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default) => throw new DelegatedException();

		public override DbConnectionCloser OpenConnection() => throw new DelegatedException();

		public override ValueTask<DbConnectionCloser> OpenConnectionAsync(CancellationToken cancellationToken = default) => throw new DelegatedException();

		public override DbTransactionDisposer BeginTransaction() => throw new DelegatedException();

		public override DbTransactionDisposer BeginTransaction(IsolationLevel isolationLevel) => throw new DelegatedException();

		public override ValueTask<DbTransactionDisposer> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new DelegatedException();

		public override ValueTask<DbTransactionDisposer> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default) => throw new DelegatedException();

		public override DbTransactionDisposer AttachTransaction(IDbTransaction transaction) => throw new DelegatedException();

		public override void CommitTransaction() => throw new DelegatedException();

		public override ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default) => throw new DelegatedException();

		public override void RollbackTransaction() => throw new DelegatedException();

		public override ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default) => throw new DelegatedException();

		public override void ReleaseConnection() => throw new DelegatedException();

		public override ValueTask ReleaseConnectionAsync() => throw new DelegatedException();

		public override void Dispose() => throw new DelegatedException();

		public override ValueTask DisposeAsync() => throw new DelegatedException();

		protected internal override DbProviderMethods ProviderMethods => throw new DelegatedException();
	}
}
