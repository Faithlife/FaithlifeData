using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests.SqlFormatting
{
	[TestFixture]
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

		[Test]
		public void AddFragments()
		{
			var (text, parameters) = Render(Sql.Format($"select {1};") + Sql.Format($"select {2};"));
			text.Should().Be("select @fdp0;select @fdp1;");
			parameters.Should().Equal(("fdp0", 1), ("fdp1", 2));
		}

		[Test]
		public void ConcatParams()
		{
			var (text, parameters) = Render(Sql.Concat(Sql.Format($"select {1};"), Sql.Format($"select {2};")));
			text.Should().Be("select @fdp0;select @fdp1;");
			parameters.Should().Equal(("fdp0", 1), ("fdp1", 2));
		}

		[Test]
		public void ConcatEnumerable()
		{
			var (text, parameters) = Render(Sql.Concat(Enumerable.Range(1, 2).Select(x => Sql.Format($"select {x};"))));
			text.Should().Be("select @fdp0;select @fdp1;");
			parameters.Should().Equal(("fdp0", 1), ("fdp1", 2));
		}

		[Test]
		public void LikePrefixParamSql()
		{
			var (text, parameters) = Render(Sql.LikePrefixParam("xy_zy"));
			text.Should().Be("@fdp0");
			parameters.Should().Equal(("fdp0", "xy\\_zy%"));
		}

		[Test]
		public void NameSql()
		{
			Invoking(() => SqlSyntax.Default.Render(Sql.Name("xyzzy"))).Should().Throw<InvalidOperationException>();
			SqlSyntax.MySql.Render(Sql.Name("x`y[z]z\"y")).Text.Should().Be("`x``y[z]z\"y`");
			SqlSyntax.Postgres.Render(Sql.Name("x`y[z]z\"y")).Text.Should().Be("\"x`y[z]z\"\"y\"");
			SqlSyntax.SqlServer.Render(Sql.Name("x`y[z]z\"y")).Text.Should().Be("[x`y[z]]z\"y]");
			SqlSyntax.Sqlite.Render(Sql.Name("x`y[z]z\"y")).Text.Should().Be("\"x`y[z]z\"\"y\"");
		}

		[Test]
		public void ColumnNamesAndValuesSql()
		{
			var syntax = SqlSyntax.MySql;

			syntax.Render(Sql.ColumnNames<ItemDto>()).Text.Should().Be("`ItemId`, `DisplayName`");
			syntax.Render(Sql.ColumnNames(typeof(ItemDto))).Text.Should().Be("`ItemId`, `DisplayName`");

			var item = new ItemDto { Id = 3, DisplayName = "three" };
			var (text, parameters) = syntax.Render(Sql.Format($"insert into Items ({Sql.ColumnNames(item.GetType())}) values ({Sql.ColumnParams(item)});"));
			text.Should().Be("insert into Items (`ItemId`, `DisplayName`) values (@fdp0, @fdp1);");
			parameters.Should().Equal(("fdp0", item.Id), ("fdp1", item.DisplayName));

			var anon = new { item.Id, item.DisplayName };
			(text, parameters) = syntax.Render(Sql.Format($"insert into Items ({Sql.ColumnNames(anon.GetType())}) values ({Sql.ColumnParams(anon)});"));
			text.Should().Be("insert into Items (`Id`, `DisplayName`) values (@fdp0, @fdp1);");
			parameters.Should().Equal(("fdp0", anon.Id), ("fdp1", anon.DisplayName));
		}

		[Test]
		public void TableColumnNamesAndValuesSql()
		{
			var syntax = SqlSyntax.MySql;

			syntax.Render(Sql.ColumnNames<ItemDto>("t")).Text.Should().Be("`t`.`ItemId`, `t`.`DisplayName`");
			syntax.Render(Sql.ColumnNames(typeof(ItemDto), "t")).Text.Should().Be("`t`.`ItemId`, `t`.`DisplayName`");

			var item = new ItemDto { Id = 3, DisplayName = "three" };
			syntax.Render(Sql.ColumnNames(item.GetType(), "t")).Text.Should().Be("`t`.`ItemId`, `t`.`DisplayName`");
		}

		[Test]
		public void SnakeCaseNamesAndValuesSql()
		{
			var syntax = SqlSyntax.MySql.WithSnakeCase();

			syntax.Render(Sql.ColumnNames<ItemDto>("t")).Text.Should().Be("`t`.`ItemId`, `t`.`display_name`");
			syntax.Render(Sql.ColumnNames(typeof(ItemDto), "t")).Text.Should().Be("`t`.`ItemId`, `t`.`display_name`");

			var item = new ItemDto { Id = 3, DisplayName = "three" };
			syntax.Render(Sql.ColumnNames(item.GetType(), "t")).Text.Should().Be("`t`.`ItemId`, `t`.`display_name`");

			var anon = new { item.Id, item.DisplayName };
			syntax.Render(Sql.ColumnNames(anon.GetType(), "t")).Text.Should().Be("`t`.`id`, `t`.`display_name`");
		}

		[Test]
		public void TupleColumnNamesAndValuesSql()
		{
			var syntax = SqlSyntax.MySql;

			syntax.Render(Sql.ColumnNames<(ItemDto, ItemDto)>()).Text.Should().Be("`ItemId`, `DisplayName`, NULL, `ItemId`, `DisplayName`");
			syntax.Render(Sql.ColumnNames(typeof((ItemDto, ItemDto)), "t1")).Text.Should().Be("`t1`.`ItemId`, `t1`.`DisplayName`, NULL, `ItemId`, `DisplayName`");
			syntax.Render(Sql.ColumnNames<(ItemDto, ItemDto)>("t1", "t2")).Text.Should().Be("`t1`.`ItemId`, `t1`.`DisplayName`, NULL, `t2`.`ItemId`, `t2`.`DisplayName`");
			syntax.Render(Sql.ColumnNames(typeof((ItemDto, ItemDto)), "", "t2")).Text.Should().Be("`ItemId`, `DisplayName`, NULL, `t2`.`ItemId`, `t2`.`DisplayName`");
		}

		private static (string Text, DbParameters Parameters) Render(Sql sql) => SqlSyntax.Default.Render(sql);

		private sealed class ItemDto
		{
			[Column("ItemId")]
			public int Id { get; set; }

			public string? DisplayName { get; set; }
		}
	}
}
