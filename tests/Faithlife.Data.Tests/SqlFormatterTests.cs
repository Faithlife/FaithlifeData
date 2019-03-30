using System;
using System.Diagnostics.CodeAnalysis;
using Faithlife.Data.SqlSyntax;
using FluentAssertions;
using NUnit.Framework;
using static Faithlife.Data.Tests.FluentAction;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	[SuppressMessage("ReSharper", "InterpolatedStringExpressionIsNotIFormattable")]
	public class SqlFormatterTests
	{
		[Test]
		public void StringLiterals()
		{
			Sql.RenderString("text").Should().Be("'text'");
			Sql.RenderString("").Should().Be("''");
			Sql.RenderString("Bob's").Should().Be("'Bob''s'");
			Invoking(() => Sql.RenderString(null)).Should().Throw<ArgumentNullException>();
		}

		[Test]
		public void BooleanLiterals()
		{
			Sql.RenderBoolean(true).Should().Be("1");
			Sql.RenderBoolean(false).Should().Be("0");
		}

		[Test]
		public void ObjectLiterals()
		{
			Sql.RenderLiteral(null).Should().Be("NULL");
			Sql.RenderLiteral("text").Should().Be("'text'");
			Sql.RenderLiteral(true).Should().Be("1");
			Sql.RenderLiteral(42).Should().Be("42");
			Sql.RenderLiteral(-42L).Should().Be("-42");
			Sql.RenderLiteral(short.MinValue).Should().Be("-32768");
			Sql.RenderLiteral(3.14f).Should().Be("3.14");
			Sql.RenderLiteral(3.1415).Should().Be("3.1415");
			Sql.RenderLiteral(867.5309m).Should().Be("867.5309");
			Invoking(() => Sql.RenderLiteral(new object())).Should().Throw<ArgumentException>();
		}

		[Test]
		public void FormatSql()
		{
			int id = 123;
			string name = "it's";
			string select = "select * from widgets";
			Sql.Format($"{select:raw} where id = {id:literal} and name = {name:literal}")
				.Should().Be("select * from widgets where id = 123 and name = 'it''s'");
		}

		[Test]
		public void RawMustBeString()
		{
			Invoking(() => Sql.Format($"select * from widgets where created = {DateTime.UtcNow:raw}"))
				.Should().Throw<FormatException>();
		}

		[Test]
		public void MissingFormat()
		{
			string name = "it's";
			Invoking(() => Sql.Format($"select * from widgets where name = {name}"))
				.Should().Throw<FormatException>();
		}

		[Test]
		public void UnknownFormat()
		{
			string name = "it's";
			Invoking(() => Sql.Format($"select * from widgets where name = {name:liberal}"))
				.Should().Throw<FormatException>();
		}

		private static SqlFormatter Sql { get; } = SqlFormatter.Default;
	}
}
