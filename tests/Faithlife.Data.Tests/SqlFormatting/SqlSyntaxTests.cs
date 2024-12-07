using System.ComponentModel.DataAnnotations.Schema;
using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests.SqlFormatting;

#pragma warning disable FL0014 // Interpolated strings for literals

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
	public void ParamOfSql()
	{
		Invoking(() => Render(Sql.Param(Sql.Raw("xyzzy")))).Should().Throw<ArgumentException>();
	}

	[Test]
	public void ListSql()
	{
		var (text, parameters) = Render(Sql.List(Sql.Param("one"), Sql.Param("two"), Sql.Raw("null")));
		text.Should().Be("@fdp0, @fdp1, null");
		parameters.Should().Equal(("fdp0", "one"), ("fdp1", "two"));
	}

	[Test]
	public void ListNone()
	{
		var (text, parameters) = Render(Sql.List());
		text.Should().BeEmpty();
		parameters.Should().BeEmpty();
	}

	[Test]
	public void TupleSql()
	{
		var (text, parameters) = Render(Sql.Tuple(Sql.Param("one"), Sql.Param("two"), Sql.Raw("null")));
		text.Should().Be("(@fdp0, @fdp1, null)");
		parameters.Should().Equal(("fdp0", "one"), ("fdp1", "two"));
	}

	[Test]
	public void TupleNone()
	{
		var (text, parameters) = Render(Sql.Tuple());
		text.Should().Be("()");
		parameters.Should().BeEmpty();
	}

	[Test]
	public void ParamListSqlStrings()
	{
		var (text, parameters) = Render(Sql.ParamList(["one", "two", "three"]));
		text.Should().Be("@fdp0, @fdp1, @fdp2");
		parameters.Should().Equal(("fdp0", "one"), ("fdp1", "two"), ("fdp2", "three"));
	}

	[Test]
	public void ParamListSqlNumbers()
	{
		var (text, parameters) = Render(Sql.ParamList(new[] { 1, 2 }));
		text.Should().Be("@fdp0, @fdp1");
		parameters.Should().Equal(("fdp0", 1), ("fdp1", 2));
	}

	[Test]
	public void ParamListSqlMixedNumbers()
	{
		var (text, parameters) = Render(Sql.ParamList([1, 2L]));
		text.Should().Be("@fdp0, @fdp1");
		parameters.Should().Equal(("fdp0", 1), ("fdp1", 2L));
	}

	[Test]
	public void ParamListSqlMixedObjects()
	{
		var (text, parameters) = Render(Sql.ParamList(["one", 2, null]));
		text.Should().Be("@fdp0, @fdp1, @fdp2");
		parameters.Should().Equal(("fdp0", "one"), ("fdp1", 2), ("fdp2", null));
	}

	[Test]
	public void ParamListNone()
	{
		var (text, parameters) = Render(Sql.ParamList([]));
		text.Should().BeEmpty();
		parameters.Should().BeEmpty();
	}

	[Test]
	public void ParamTupleSqlStrings()
	{
		var (text, parameters) = Render(Sql.ParamTuple(["one", "two", "three"]));
		text.Should().Be("(@fdp0, @fdp1, @fdp2)");
		parameters.Should().Equal(("fdp0", "one"), ("fdp1", "two"), ("fdp2", "three"));
	}

	[Test]
	public void ParamTupleSqlNumbers()
	{
		var (text, parameters) = Render(Sql.ParamTuple(new[] { 1, 2 }));
		text.Should().Be("(@fdp0, @fdp1)");
		parameters.Should().Equal(("fdp0", 1), ("fdp1", 2));
	}

	[Test]
	public void ParamTupleSqlMixedNumbers()
	{
		var (text, parameters) = Render(Sql.ParamTuple([1, 2L]));
		text.Should().Be("(@fdp0, @fdp1)");
		parameters.Should().Equal(("fdp0", 1), ("fdp1", 2L));
	}

	[Test]
	public void ParamTupleSqlMixedObjects()
	{
		var (text, parameters) = Render(Sql.ParamTuple(["one", 2, null]));
		text.Should().Be("(@fdp0, @fdp1, @fdp2)");
		parameters.Should().Equal(("fdp0", "one"), ("fdp1", 2), ("fdp2", null));
	}

	[Test]
	public void ParamTupleNone()
	{
		var (text, parameters) = Render(Sql.ParamTuple([]));
		text.Should().Be("()");
		parameters.Should().BeEmpty();
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
	public void SameParamTwice()
	{
		var id = 42;
		var name = "xyzzy";
		var desc = "long description";
		var descParam = Sql.Param(desc);
		var (text, parameters) = Render(Sql.Format($"insert into widgets (Id, Name, Desc) values ({id}, {name}, {descParam}) on duplicate key update Name = {name}, Desc = {descParam}"));
		text.Should().Be("insert into widgets (Id, Name, Desc) values (@fdp0, @fdp1, @fdp2) on duplicate key update Name = @fdp3, Desc = @fdp2");
		parameters.Should().Equal(("fdp0", id), ("fdp1", name), ("fdp2", desc), ("fdp3", name));
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
			if (width is not null)
				sqls.Add(Sql.Format($"width = {width}"));
			if (height is not null)
				sqls.Add(Sql.Format($"height = {height}"));
			var whereSql = sqls.Count == 0 ? Sql.Empty : Sql.Format($"where {Sql.Join(" and ", sqls)}");
			return Sql.Format($"select * from widgets {whereSql};");
		}
	}

	[Test]
	public void JoinEmpty()
	{
		var (text, parameters) = Render(Sql.Join("/", Sql.Raw("one"), Sql.Empty, Sql.Raw("two")));
		text.Should().Be("one/two");
		parameters.Should().BeEmpty();
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
	public void ColumnNamesAndValuesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.ColumnNamesWhere<ItemDto>(x => x != nameof(ItemDto.Id))).Text.Should().Be("`DisplayName`");

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = syntax.Render(Sql.Format($@"
				insert into Items ({Sql.ColumnNamesWhere(item.GetType(), x => x is not nameof(ItemDto.Id))})
				values ({Sql.ColumnParamsWhere(item, x => x is not nameof(ItemDto.Id))});"));
		text.Should().Be(@"
				insert into Items (`DisplayName`)
				values (@fdp0);");
		parameters.Should().Equal(("fdp0", item.DisplayName));
	}

	[Test]
	public void TableColumnNamesAndValuesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.ColumnNamesWhere<ItemDto>(x => x != nameof(ItemDto.Id), "t")).Text.Should().Be("`t`.`DisplayName`");

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = syntax.Render(Sql.Format($@"
				insert into Items ({Sql.ColumnNamesWhere(item.GetType(), x => x is not nameof(ItemDto.Id), "t")})
				values ({Sql.ColumnParamsWhere(item, x => x is not nameof(ItemDto.Id))});"));
		text.Should().Be(@"
				insert into Items (`t`.`DisplayName`)
				values (@fdp0);");
		parameters.Should().Equal(("fdp0", item.DisplayName));
	}

	[Test]
	public void ColumnNamesAndValuesWhereNoneException()
	{
		var syntax = SqlSyntax.MySql;

		Invoking(() => syntax.Render(Sql.ColumnNamesWhere<ItemDto>(_ => false))).Should().Throw<InvalidOperationException>();
		Invoking(() => syntax.Render(Sql.ColumnParamsWhere(new ItemDto(), _ => false))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void TupleColumnNamesSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.ColumnNames<(ItemDto, ItemDto)>()).Text.Should().Be("`ItemId`, `DisplayName`, NULL, `ItemId`, `DisplayName`");
		syntax.Render(Sql.ColumnNames(typeof((ItemDto, ItemDto)), "t1")).Text.Should().Be("`t1`.`ItemId`, `t1`.`DisplayName`, NULL, `ItemId`, `DisplayName`");
		syntax.Render(Sql.ColumnNames<(ItemDto, ItemDto)>("t1", "t2")).Text.Should().Be("`t1`.`ItemId`, `t1`.`DisplayName`, NULL, `t2`.`ItemId`, `t2`.`DisplayName`");
		syntax.Render(Sql.ColumnNames(typeof((ItemDto, ItemDto)), "", "t2")).Text.Should().Be("`ItemId`, `DisplayName`, NULL, `t2`.`ItemId`, `t2`.`DisplayName`");

		static bool NotId(string propertyName) => propertyName != nameof(ItemDto.Id);
		syntax.Render(Sql.ColumnNamesWhere<(ItemDto, ItemDto)>(NotId)).Text.Should().Be("`DisplayName`, NULL, `DisplayName`");
		syntax.Render(Sql.ColumnNamesWhere(typeof((ItemDto, ItemDto)), NotId, "t1")).Text.Should().Be("`t1`.`DisplayName`, NULL, `DisplayName`");
		syntax.Render(Sql.ColumnNamesWhere<(ItemDto, ItemDto)>(NotId, "t1", "t2")).Text.Should().Be("`t1`.`DisplayName`, NULL, `t2`.`DisplayName`");
		syntax.Render(Sql.ColumnNamesWhere(typeof((ItemDto, ItemDto)), NotId, "", "t2")).Text.Should().Be("`DisplayName`, NULL, `t2`.`DisplayName`");
	}

	[Test]
	public void DtoParamNamesSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.DtoParamNames<ItemDto>()).Text.Should().Be("@Id, @DisplayName");
		syntax.Render(Sql.DtoParamNames<ItemDto>("p")).Text.Should().Be("@p_Id, @p_DisplayName");
		syntax.Render(Sql.DtoParamNames<ItemDto>(x => x + "_")).Text.Should().Be("@Id_, @DisplayName_");
	}

	[Test]
	public void DtoParamNamesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.DtoParamNamesWhere<ItemDto>(NotId)).Text.Should().Be("@DisplayName");
		syntax.Render(Sql.DtoParamNamesWhere<ItemDto>("p", NotId)).Text.Should().Be("@p_DisplayName");
		syntax.Render(Sql.DtoParamNamesWhere<ItemDto>(x => x + "_", NotId)).Text.Should().Be("@DisplayName_");

		static bool NotId(string x) => x != nameof(ItemDto.Id);
	}

	[TestCase("", "")]
	[TestCase("one", "one")]
	[TestCase("one,two", "one AND two")]
	[TestCase("one,two,three", "one and two and three", true)]
	public void And(string values, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.And(values.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Should().BeEmpty();
	}

	[TestCase("", "")]
	[TestCase("one", "one")]
	[TestCase("one,two", "one OR two")]
	[TestCase("one,two,three", "one or two or three", true)]
	public void Or(string values, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.Or(values.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Should().BeEmpty();
	}

	[Test]
	public void AndOrAnd()
	{
		var (text, parameters) = Render(Sql.And(Sql.Raw("one"), Sql.Or(Sql.Raw("two"), Sql.And(Sql.Raw("three")))));
		text.Should().Be("one AND (two OR three)");
		parameters.Should().BeEmpty();
	}

	[TestCase("", "")]
	[TestCase("true", "WHERE true")]
	[TestCase("true", "where true", true)]
	public void Where(string condition, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.Where(Sql.Raw(condition)));
		text.Should().Be(sql);
		parameters.Should().BeEmpty();
	}

	[TestCase("", "")]
	[TestCase("x asc", "ORDER BY x asc")]
	[TestCase("x asc;y desc", "order by x asc, y desc", true)]
	public void OrderBy(string columns, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.OrderBy(columns.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Should().BeEmpty();
	}

	[TestCase("", "")]
	[TestCase("x", "GROUP BY x")]
	[TestCase("x;y", "group by x, y", true)]
	public void GroupBy(string columns, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.GroupBy(columns.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Should().BeEmpty();
	}

	[TestCase("", "")]
	[TestCase("x < 1", "HAVING x < 1")]
	[TestCase("x < 1", "having x < 1", true)]
	public void Having(string condition, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.Having(Sql.Raw(condition)));
		text.Should().Be(sql);
		parameters.Should().BeEmpty();
	}

	private static (string Text, DbParameters Parameters) Render(Sql sql) => SqlSyntax.Default.Render(sql);

	private sealed class ItemDto
	{
		[Column("ItemId")]
		public int Id { get; set; }

		public string? DisplayName { get; set; }
	}
}
