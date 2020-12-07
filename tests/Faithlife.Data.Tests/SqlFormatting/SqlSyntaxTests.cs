using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

		[Test]
		public void EmptySql()
		{
			var (text, parameters) = Render(Sql.Empty);
			text.Should().Be("");
			parameters.Should().BeEmpty();
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
			var whereSql = id is null ? Sql.Empty : Sql.Format($"where id = {Sql.Param(id)}");
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
		public void JoinParams()
		{
			var (text, parameters) = Render(Sql.Join(", ", Sql.Param(42), Sql.Param(-42)));
			text.Should().Be("@fdp0, @fdp1");
			parameters.Should().Equal(("fdp0", 42), ("fdp1", -42));
		}

		[Test]
		public void JoinEnumerable()
		{
			Render(CreateSql(42, 24)).Text.Should().Be("select * from widgets where width = @fdp0 and height = @fdp1;");
			Render(CreateSql(null, 24)).Text.Should().Be("select * from widgets where height = @fdp0;");
			Render(CreateSql(null, null)).Text.Should().Be("select * from widgets ;");

			Sql CreateSql(int? width, int? height)
			{
				var sqls = new List<Sql>();
				if (width != null)
					sqls.Add(Sql.Format($"width = {width}"));
				if (height != null)
					sqls.Add(Sql.Format($"height = {height}"));
				var whereSql = sqls.Count == 0 ? Sql.Empty : Sql.Format($"where {Sql.Join(" and ", sqls)}");
				return Sql.Format($"select * from widgets {whereSql};");
			}
		}

		private static (string Text, DbParameters Parameters) Render(Sql sql) => SqlSyntax.Default.Render(sql);
	}
}
