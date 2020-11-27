using System;
using System.Diagnostics.CodeAnalysis;
using Faithlife.Data.SqlText;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests.SqlText
{
	[TestFixture]
	[SuppressMessage("ReSharper", "InterpolatedStringExpressionIsNotIFormattable", Justification = "Custom formatting.")]
	public class SqlRendererTests
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
		public void FormatRawArg()
		{
			var tableName = "widgets";
			var (text, parameters) = Render(Sql.Format($"select * from {tableName:raw}"));
			text.Should().Be("select * from widgets");
			parameters.Should().BeEmpty();
		}

		[Test]
		public void FormatRawArgNoFormat()
		{
			var tableName = "widgets";
			Invoking(() => Render(Sql.Format($"select * from {tableName}"))).Should().Throw<FormatException>();
		}

		[Test]
		public void FormatSqlRawArgNotString()
		{
			var tableName = Sql.Raw("widgets");
			Invoking(() => Render(Sql.Format($"select * from {tableName:raw}"))).Should().Throw<FormatException>();
		}

		[Test]
		public void FormatSqlArg()
		{
			var tableName = Sql.Raw("widgets");
			var (text, parameters) = Render(Sql.Format($"select * from {tableName}"));
			text.Should().Be("select * from widgets");
			parameters.Should().BeEmpty();
		}

		[Test]
		public void FormatSqlBadFormat()
		{
			var tableName = "widgets";
			Invoking(() => Render(Sql.Format($"select * from {tableName:xyzzy}"))).Should().Throw<FormatException>();
		}

		private static (string Text, DbParameters Parameters) Render(Sql sql) => SqlRenderer.Default.Render(sql);
	}
}
