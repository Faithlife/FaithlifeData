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
		public void FormatExplicitParam()
		{
			var (text, parameters) = Render(Sql.Format($"select * from widgets where id in ({42:param}, {-42:param})"));
			text.Should().Be("select * from widgets where id in (@fdp0, @fdp1)");
			parameters.Should().Equal(("fdp0", 42), ("fdp1", -42));
		}

		[Test]
		public void FormatImplicitParam()
		{
			var (text, parameters) = Render(Sql.Format($"select * from widgets where id in ({42}, {-42})"));
			text.Should().Be("select * from widgets where id in (@fdp0, @fdp1)");
			parameters.Should().Equal(("fdp0", 42), ("fdp1", -42));
		}

		[TestCase(null)]
		[TestCase(42)]
		public void FormatSql(int? id)
		{
			var whereSql = id is null ? Sql.Raw("") : Sql.Format($"where id = {Sql.Param(id)}");
			var limit = 10;
			var (text, parameters) = Render(Sql.Format($"select * from {Sql.Raw("widgets")} {whereSql} limit {limit}"));
			if (id is null)
			{
				text.Should().Be("select * from widgets  limit @fdp0");
				parameters.Should().Equal(("fdp0", limit));
			}
			else
			{
				text.Should().Be("select * from widgets where id = @fdp0 limit @fdp1");
				parameters.Should().Equal(("fdp0", id), ("fdp1", limit));
			}
		}

		[Test]
		public void FormatBadFormat()
		{
			var tableName = "widgets";
			Invoking(() => Render(Sql.Format($"select * from {tableName:xyzzy}"))).Should().Throw<FormatException>();
		}

		[Test]
		public void LikePrefixParamSql()
		{
			var (text, parameters) = Render(Sql.LikePrefixParam("xy_zy"));
			text.Should().Be("@fdp0");
			parameters.Should().Equal(("fdp0", "xy\\_zy%"));
		}

		private static (string Text, DbParameters Parameters) Render(Sql sql) => SqlSyntax.Default.Render(sql);
	}
}
