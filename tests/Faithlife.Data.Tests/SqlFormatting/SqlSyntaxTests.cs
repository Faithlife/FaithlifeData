using System;
using System.Diagnostics.CodeAnalysis;
using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests.SqlFormatting
{
	[TestFixture]
	[SuppressMessage("ReSharper", "InterpolatedStringExpressionIsNotIFormattable", Justification = "Custom formatting.")]
	public class SqlSyntaxTests
	{
		[Test]
		public void NullSqlThrows()
		{
			Invoking(() => Render(null!)).Should().Throw<ArgumentNullException>();
		}

		[TestCase("")]
		[TestCase("select * from widgets")]
		public void RawSql(string raw)
		{
			var (text, parameters) = Render(Sql.Raw(raw));
			text.Should().Be(raw);
			parameters.Should().BeEmpty();
		}

		[Test]
		public void ParamSql()
		{
			var (text, parameters) = Render(Sql.Param("xyzzy"));
			text.Should().Be("@fdp0");
			parameters.Should().Equal(("fdp0", "xyzzy"));
		}

		[Test]
		public void FormatEmpty()
		{
			var (text, parameters) = Render(Sql.Format($""));
			text.Should().Be("");
			parameters.Should().BeEmpty();
		}

		[Test]
		public void FormatNoArgs()
		{
			var (text, parameters) = Render(Sql.Format($"select * from widgets"));
			text.Should().Be("select * from widgets");
			parameters.Should().BeEmpty();
		}

		[Test]
		public void FormatRaw()
		{
			var tableName = "widgets";
			var (text, parameters) = Render(Sql.Format($"select * from {tableName:raw}"));
			text.Should().Be("select * from widgets");
			parameters.Should().BeEmpty();
		}

		[Test]
		public void FormatSqlRawNotString()
		{
			var tableName = Sql.Raw("widgets");
			Invoking(() => Render(Sql.Format($"select * from {tableName:raw}"))).Should().Throw<FormatException>();
		}

		[Test]
		public void FormatParams()
		{
			var (text, parameters) = Render(Sql.Format($"select * from widgets where id in ({42:param}, {-42:param})"));
			text.Should().Be("select * from widgets where id in (@fdp0, @fdp1)");
			parameters.Should().Equal(("fdp0", 42), ("fdp1", -42));
		}

		[Test]
		public void FormatSql()
		{
			var (text, parameters) = Render(Sql.Format($"select * from {Sql.Raw("widgets")} where id = {Sql.Param(42)}"));
			text.Should().Be("select * from widgets where id = @fdp0");
			parameters.Should().Equal(("fdp0", 42));
		}

		[Test]
		public void FormatMissingFormat()
		{
			var tableName = "widgets";
			Invoking(() => Render(Sql.Format($"select * from {tableName}"))).Should().Throw<FormatException>();
		}

		[Test]
		public void FormatBadFormat()
		{
			var tableName = "widgets";
			Invoking(() => Render(Sql.Format($"select * from {tableName:xyzzy}"))).Should().Throw<FormatException>();
		}

		private static (string Text, DbParameters Parameters) Render(Sql sql) => SqlSyntax.Default.Render(sql);
	}
}
