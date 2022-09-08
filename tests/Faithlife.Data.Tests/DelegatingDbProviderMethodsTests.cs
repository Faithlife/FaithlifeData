using System.Data;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests;

[TestFixture]
public class DelegatingDbProviderMethodsTests
{
	[Test]
	public void DelegateAllVirtuals()
	{
		var methods = typeof(DbProviderMethods).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Concat(typeof(DbProviderMethods).GetProperties().Select(x => x.GetMethod!))
			.Where(x => x.IsAbstract || x.IsVirtual)
			.ToList();
		var delegated = new DelegatedDbProviderMethods();
		InvokeMethods(delegated);
		var delegating = new DelegatingDbProviderMethods(delegated);
		InvokeMethods(delegating);

		void InvokeMethods(DbProviderMethods connector)
		{
			foreach (var method in methods)
			{
				Invoking(() => method.Invoke(connector, new object[method.GetParameters().Length]))
					.Should().Throw<TargetInvocationException>(method.Name).WithInnerException<DelegatedException>(method.Name);
			}
		}
	}

	private sealed class DelegatedDbProviderMethods : DbProviderMethods
	{
		public override void OpenConnection(IDbConnection connection) => throw new DelegatedException();

		public override ValueTask OpenConnectionAsync(IDbConnection connection, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask CloseConnectionAsync(IDbConnection connection) => throw new DelegatedException();

		public override ValueTask DisposeConnectionAsync(IDbConnection connection) => throw new DelegatedException();

		public override ValueTask<IDbTransaction> BeginTransactionAsync(IDbConnection connection, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask<IDbTransaction> BeginTransactionAsync(IDbConnection connection, IsolationLevel isolationLevel, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask CommitTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask RollbackTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask DisposeTransactionAsync(IDbTransaction transaction) => throw new DelegatedException();

		public override ValueTask<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask<IDataReader> ExecuteReaderAsync(IDbCommand command, CommandBehavior commandBehavior, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask PrepareCommandAsync(IDbCommand command, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask DisposeCommandAsync(IDbCommand command) => throw new DelegatedException();

		public override ValueTask<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask<bool> NextResultAsync(IDataReader reader, CancellationToken cancellationToken) => throw new DelegatedException();

		public override ValueTask DisposeReaderAsync(IDataReader reader) => throw new DelegatedException();
	}
}
