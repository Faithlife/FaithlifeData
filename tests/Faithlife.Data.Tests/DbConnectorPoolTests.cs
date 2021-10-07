using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class DbConnectorPoolTests
	{
		[Test]
		public void NullSettings()
		{
			Invoking(() => new DbConnectorPool(null!)).Should().Throw<ArgumentNullException>();
		}

		[Test]
		public void NoCreate()
		{
			Invoking(() => new DbConnectorPool(new DbConnectorPoolSettings())).Should().Throw<ArgumentException>();
		}

		[Test]
		public void Sync()
		{
			var createCount = 0;

			using var pool = new DbConnectorPool(new DbConnectorPoolSettings { Create = CreateConnection });

			using (var connector1 = pool.Get())
			using (var connector2 = pool.Get())
			{
				connector1.Command("select null;").QuerySingle<object>().Should().Be(null);
				connector2.Command("select null;").QuerySingle<object>().Should().Be(null);
			}
			using (var connector3 = pool.Get())
				connector3.Command("select null;").QuerySingle<object>().Should().Be(null);

			createCount.Should().Be(2);

			DbConnector CreateConnection()
			{
				createCount++;
				return DbConnector.Create(
					new SqliteConnection("Data Source=:memory:"),
					new DbConnectorSettings { AutoOpen = true });
			}
		}

		[Test]
		public async Task Async()
		{
			var createCount = 0;

			await using var pool = new DbConnectorPool(new DbConnectorPoolSettings { Create = CreateConnection });

			await using (var connector1 = pool.Get())
			await using (var connector2 = pool.Get())
			{
				(await connector1.Command("select null;").QuerySingleAsync<object>()).Should().Be(null);
				(await connector2.Command("select null;").QuerySingleAsync<object>()).Should().Be(null);
			}
			await using (var connector3 = pool.Get())
				(await connector3.Command("select null;").QuerySingleAsync<object>()).Should().Be(null);

			createCount.Should().Be(2);

			DbConnector CreateConnection()
			{
				createCount++;
				return DbConnector.Create(
					new SqliteConnection("Data Source=:memory:"),
					new DbConnectorSettings { AutoOpen = true });
			}
		}
	}
}
