# Faithlife.Data

**Faithlife.Data** provides helpers for querying ADO.NET-compatible databases.

[![NuGet](https://img.shields.io/nuget/v/Faithlife.Data.svg)](https://www.nuget.org/packages/Faithlife.Data)

## Quick Start

The **Faithlife.Data** class library provides an enhanced API for interacting with ADO.NET-compatible databases. It is similar to [Dapper](https://github.com/StackExchange/Dapper) and other "micro" ORMs for .NET.

To use this library, add a [NuGet package reference](https://www.nuget.org/packages/Faithlife.Data) to your project and call [`DbConnector.Create()`](Faithlife.Data/DbConnector/Create.md) to create a [`DbConnector`](Faithlife.Data/DbConnector.md) with a valid [`IDbConnection`](https://docs.microsoft.com/dotnet/api/system.data.idbconnection) from your favorite ADO.NET database provider, e.g. [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) for SQL Server or [MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) for MySQL.

Here's a simple code sample that opens an in-memory SQLite database, creates a table, inserts a few rows within a transaction, and runs a couple of queries. There's no risk of SQL injection attacks with the interpolated strings, which use [formatted SQL](#formatted-sql), documented below. [**Try it!**](https://dotnetfiddle.net/86HLqI)

```csharp
// create connection; open automatically and just in time
await using var connector = DbConnector.Create(
  new SqliteConnection("Data Source=:memory:"),
  new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

// create widgets table
await connector.Command(@"
    create table widgets (
    id integer primary key autoincrement,
    name text not null,
    height real not null)")
  .ExecuteAsync();

// insert widgets in a transaction
var widgets = new[]
{
  new Widget("First", 6.875),
  new Widget("Second", 1.414),
  new Widget("Third", 3.1415),
};
await using (await connector.BeginTransactionAsync())
{
  foreach (var widget in widgets)
  {
    await connector.Command(Sql.Format($@"
        insert into widgets (name, height)
        values ({widget.Name}, {widget.Height})"))
      .Cache().ExecuteAsync();
  }
  await connector.CommitTransactionAsync();
}

// get short widgets
var maxHeight = 5.0;
foreach (var widget in await connector
  .Command(Sql.Format(
    $"select name, height from widgets where height <= {maxHeight}"))
  .QueryAsync<Widget>())
{
  Console.WriteLine(widget.ToString());
}

// get minimum and maximum heights
var (min, max) = await connector
  .Command("select min(height), max(height) from widgets")
  .QuerySingleAsync<(double, double)>();
Console.WriteLine($"minimum height {min}, maximum height {max}");
```

## Overview

With a `DbConnector`, you can:

* Automatically open the connection and/or wait to open the connection until it is needed with [`DbConnectorSettings`](Faithlife.Data/DbConnectorSettings.md).
* Begin, commit, and rollback transactions with [`DbConnector.BeginTransaction()`](Faithlife.Data/DbConnector/BeginTransaction.md), [`DbConnector.CommitTransaction()`](Faithlife.Data/DbConnector/CommitTransaction.md), etc.
* Create and execute database commands, automatically using any current transaction, with  [`DbConnector.Command()`](Faithlife.Data/DbConnector/Command.md) followed by [`Execute()`](Faithlife.Data/DbConnectorCommand/Execute.md), [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md), etc.
* Provide named command parameters from name/value tuples and/or DTO properties with  [`DbParameters`](Faithlife.Data/DbParameters.md).
* Efficiently map database records into simple data types, DTOs, and/or tuples ([details below](#mapping-database-records)).
* Read all records at once with [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md), or read records one at a time with [`Enumerate()`](Faithlife.Data/DbConnectorCommand/Enumerate.md).
* Efficiently access only the first record of the query result with [`QueryFirst()`](Faithlife.Data/DbConnectorCommand/QueryFirst.md), [`QuerySingleOrDefault()`](Faithlife.Data/DbConnectorCommand/QuerySingleOrDefault.md), etc.
* Access the database synchronously or asynchronously with cancellation support, e.g. [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md) vs. [`QueryAsync()`](Faithlife.Data/DbConnectorCommand/QueryAsync.md).
* Read multiple result sets from multi-statement commands with [`QueryMultiple()`](Faithlife.Data/DbConnectorCommand/QueryMultiple.md).
* Expand [collection parameters](#collection-parameters) to a list of numbered parameters for easier `IN` support.
* Use [bulk insert](#bulk-insert) to easily and efficiently insert multiple rows into a table.
* Execute stored procedures with [`DbConnector.StoredProcedure()`](Faithlife.Data/DbConnector/StoredProcedure.md).
* [Cache](#cached-commands) and/or [prepare](#prepared-commands) commands for possible performance improvements.
* Use [formatted SQL](#formatted-sql) to use automatically named parameters via string interpolation and safely assemble SQL statements from SQL fragments.

Consult the [reference documentation](Faithlife.Data.md) for additional details.

### What about Dapper?

If you are familiar with [Dapper](https://github.com/StackExchange/Dapper), you will note many similarities between it and this library. So why use Faithlife.Data? Here are a few key differences:

* `DbConnector` **wraps the connection**, whereas Dapper primarly provides extension methods on `IDbConnection`.
* With Dapper, you must remember to set the `transaction` parameter when there is an active transaction. Since Faithlife.Data **tracks the current transaction**, it attaches it to database commands automatically.
* Faithlife.Data has direct support for **modern C# and .NET**, including tuples, `IAsyncEnumerable`, and the new index/range syntax.
* The **multi-mapping support** of Faithlife.Data is simpler and more flexible than the `map` and `splitOn` parameters of Dapper.
* Faithlife.Data **avoids type conversion**, requiring that the requested type exactly match the provided type, whereas Dapper will try to convert the value with [`Convert.ChangeType()`](https://docs.microsoft.com/dotnet/api/system.convert.changetype). This is sometimes aggravating, but we feel it is better to know what the database is returning and avoid the surprises that type conversion can bring.
* The **async methods** of Faithlife.Data call the async methods of the database provider more consistently than Dapper.
* Faithlife.Data makes the choice between **buffered and unbuffered queries** more explicit by providing separate methods. This makes it more likely that clients will keep the difference in mind, and allows `Query()` to return an `IReadOnlyList<T>` instead of an `IEnumerable<T>`.
* Faithlife.Data has an easy **alternative to using anonymous objects** for specifying parameters, which may have better performance for some clients and uses stronger types than Dapper's `param` parameter of type `object`.
* Faithlife.Data does **less caching** than Dapper. This may or may not be an advantage, depending on usage.
* Both Faithlife.Data and Dapper will edit the SQL when substituting a collection parameter for a list of dynamically named parameters. The syntax used by Faithlife.Data is more explicit, so scenarios where the SQL is edited are **more predictable**. Also, Faithlife.Data throws an exception when an empty collection is used, since the desired behavior in that scenario is not clear, and Dapper's strategy of replacing it with `(SELECT @p WHERE 1 = 0)` doesn't work with all databases, isn't always what the caller would want, and doesn't always play well with table indexes.
* Faithlife.Data has **extra features** like bulk insert and formatted SQL.

## Creating a connector

Like `IDbConnection`, `DbConnector` is *not* thread-safe, so you will need one instance per connection. Consider defining a method to easily create a connection to your database. This example uses [Microsoft.Data.Sqlite](https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/).

```csharp
DbConnector CreateConnector() =>
    DbConnector.Create(new SqliteConnection("Data Source=:memory:"));
```

If you use [formatted SQL](#formatted-sql), some features require you to specify your SQL syntax with the [`SqlSyntax`](Faithlife.Data/DbConnectorSettings/SqlSyntax.md) setting. (This is not required for simple parameter injection.)

```csharp
DbConnector CreateConnector() =>
    DbConnector.Create(new SqliteConnection("Data Source=:memory:"),
    new DbConnectorSettings { SqlSyntax = SqlSyntax.Sqlite });
```

If your database columns use `snake_case`, consider using [`SqlSyntax.WithSnakeCase()`](Faithlife.Data.SqlFormatting/SqlSyntax/WithSnakeCase.md), which causes [`Sql.ColumnNames()`](Faithlife.Data.SqlFormatting/Sql/ColumnNames.md) to generate `snake_case` column names from `PascalCase` property names.

```csharp
new DbConnectorSettings { SqlSyntax = SqlSyntax.MySql.WithSnakeCase() }
```

## Executing a command

Once you have a connector, you can open the connection with [`OpenConnectionAsync()`](Faithlife.Data/DbConnector/OpenConnectionAsync.md), create a command with [`Command()`](Faithlife.Data/DbConnector/Command.md), and execute the command with [`ExecuteAsync()`](Faithlife.Data/DbConnectorCommand/ExecuteAsync.md).

```csharp
await using (var connector = CreateConnector())
await using (await connector.OpenConnectionAsync())
{
    await connector.Command(@"
create table widgets (
  id integer primary key autoincrement,
  name text not null);").ExecuteAsync();
}
```

Note that asynchronous methods in this library return `ValueTask`, not `Task`, so be sure to [follow the relevant guidelines](https://docs.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1), e.g. don't `await` a `ValueTask` more than once.

## Automatically opening connections

Calling `OpenConnectionAsync` every time you use a connector can get tiresome. You could move the `OpenConnectionAsync` call to your connector creation method, or you can use the [`AutoOpen`](Faithlife.Data/DbConnectorSettings/AutoOpen.md) setting.

Consider using the [`LazyOpen`](Faithlife.Data/DbConnectorSettings/LazyOpen.md) setting as well, which waits to actually open the database connection until just before it is first used. This ensures that the connection is opened asynchronously (as long as you use asynchronous methods to execute commands). Also, if you have a code path that doesn't actually execute any commands, the connection will never be opened in that scenario.

```csharp
DbConnector OpenConnector() => DbConnector.Create(
    new SqliteConnection("Data Source=:memory:"),
    new DbConnectorSettings
    {
      AutoOpen = true,
      LazyOpen = true,
      SqlSyntax = SqlSyntax.Sqlite,
   });
```

A connector can also be placed into "lazy open" mode by calling [`ReleaseConnectionAsync()`](Faithlife.Data/DbConnector/ReleaseConnectionAsync.md), which closes the connection until the next time it is used. You can use this to release database resources while performing long-running work between database queries.

## Accessing the database synchronously

Every method that has communicates with the database has an synchronous equivalent without the `Async` suffix, e.g. [`Execute()`](Faithlife.Data/DbConnectorCommand/Execute.md). Consider using the synchronous methods if your ADO.NET provider doesn't actually support asynchronous I/O.

```csharp
using (var connector = CreateConnector())
using (connector.OpenConnection())
{
    connector.Command(@"
create table widgets (
  id integer primary key autoincrement,
  name text not null);").Execute();
}
```

## Using transactions

To leverage a database transaction, call [`BeginTransactionAsync()`](Faithlife.Data/DbConnector/BeginTransactionAsync.md) before executing any commands, and then call [`CommitTransactionAsync()`](Faithlife.Data/DbConnector/CommitTransactionAsync.md) before disposing the return value of `BeginTransactionAsync()`.

```csharp
await using (var connector = OpenConnector())
await using (await connector.BeginTransactionAsync())
{
    await connector.Command(@"
create table widgets (
  id integer primary key autoincrement,
  name text not null,
  height real not null);").ExecuteAsync();

    await connector.Command(@"
insert into widgets (name, height)
  values ('First', 6.875);
insert into widgets (name, height)
  values ('Second', 3.1415);").ExecuteAsync();

    await connector.CommitTransactionAsync();
}
```

If `CommitTransactionAsync()` is not called, the transaction will be rolled back when the return value of `BeginTransactionAsync()` is disposed.

ADO.NET requres that the `Transaction` property of [`IDbCommand`](https://docs.microsoft.com/dotnet/api/system.data.idbcommand) be set to the current transaction; `DbConnector` takes care of that automatically when executing commands.

## Mapping database records

The querying methods of this class library (e.g. [`QueryAsync()`](Faithlife.Data/DbConnectorCommand/QueryAsync.md)) are generic, allowing the caller to specify the type to which each database record should be mapped.

### Simple types

When selecting a single column, use a simple type as the generic parameter.

```csharp
async Task<IReadOnlyList<string>> GetWidgetNamesAsync(
    DbConnector connector,
    CancellationToken cancellationToken = default)
{
    return await connector.Command("select name from widgets;")
        .QueryAsync<string>(cancellationToken);
}
```

For compact documentation, many examples will use the synchronous API.

```csharp
IReadOnlyList<string> GetWidgetNames(DbConnector connector) =>
    connector.Command("select name from widgets;").Query<string>();
}
```

Simple types must match the type returned from [`IDataRecord.GetValue()`](https://docs.microsoft.com/dotnet/api/system.data.idatarecord.getvalue) exactly. No conversions are performed, except between nullable and non-nullable types. Be sure to use a nullable type if the value could be null, e.g. `int?`; an exception will be thrown if the field is null but the type is not nullable.

The supported simple types are `string`, `long`, `int`, `short`, `byte`, `ulong`, `uint`, `ushort`, `sbyte`, `double`, `float`, `decimal`, `bool`, `Guid`, `DateTime`, `DateTimeOffset`, and `TimeSpan`.

Use `byte[]` or `Stream` to return the bytes from a "blob" column. If a non-null `Stream` is returned, be sure to dispose it.

Enumerated types (defined with `enum` in C#) are supported like simple types, except that conversions are performed if needed. Specifically, strings are parsed to the enumerated type (ignoring case) and integral types are cast to the enumerated type.

### Tuples

Use tuples to map multiple record fields at once. Each tuple item is read from the record in order. The record field names are ignored, as are the tuple item names, if any.

```csharp
IReadOnlyList<(string Name, double Height)> GetWidgetInfo(DbConnector connector) =>
    connector.Command("select name, height from widgets;").Query<(string, double)>();
```

### DTOs

If the type isn't a simple type or a tuple, it is assumed to be a DTO (data transfer object) type, i.e. a type with properties that correspond to record fields. Both read/write and read-only properties are supported; [`DtoInfo<T>.CreateNew`](https://faithlife.github.io/FaithlifeReflection/Faithlife.Reflection/DtoInfo-1/CreateNew.html) from [Faithlife.Reflection](https://faithlife.github.io/FaithlifeReflection/) is used to create the instance.

```csharp
class WidgetDto
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public double Height { get; set; }
}
```

When a DTO type is used, a new instance of the DTO is created, and each record field is mapped to a DTO property whose name matches the field name, ignoring case and any underscores (so `full_name` would map successfully to `FullName`, for example). If the property has a `Column` attribute with a non-null `Name` property (e.g. from [System.ComponentModel.DataAnnotations](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.schema.columnattribute)), that name is used instead of the field name. Not every property of the DTO must be used, but every mapped field must have a corresponding property.

```csharp
IReadOnlyList<WidgetDto> GetWidgets(DbConnector connector) =>
    connector.Command("select id, name, height from widgets;").Query<WidgetDto>();
```

For more ways to map records, see [advanced record mapping](#advanced-record-mapping) below.

## Parameterized queries

When executing parameterized queries, the parameter values are specified with the [`DbConnector.Command()`](Faithlife.Data/DbConnector/Command.md) method. One way to specify command parameters is via one or more string/object tuples after the command SQL.

```csharp
void InsertWidget(DbConnector connector, string name, double height) =>
    connector.Command(
        "insert into widgets (name, height) values (@name, @height);",
        ("name", name), ("height", height)).Execute();
```

### Formatted SQL

Typing parameter names in the SQL command text and parameters objects seems redundant. [String interpolation](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated) can be used to put the parameters in the SQL safely by using [`Sql.Format()`](Faithlife.Data.SqlFormatting/Sql/Format.md) to format an interpolated string into command text and parameters.

```csharp
void InsertWidget(DbConnector connector, string name, double height) =>
    connector.Command(Sql.Format(
        $"insert into widgets (name, height) values ({name}, {height});"
        )).Execute();
```

This is equivalent to the following:

```csharp
void InsertWidget(DbConnector connector, string name, double height) =>
    connector.Command(
        "insert into widgets (name, height) values (@fdp0, @fdp1);",
        ("fdp0", name), ("fdp1", height)).Execute();
```

(`fdp` is just an arbitrary prefix for the automatically named parameters; it stands for "Faithlife.Data parameter".)

Note that using an interpolated string without `Sql.Format()` is still a SQL injection vulnerability. Consider enabling [FL0012](https://github.com/Faithlife/FaithlifeAnalyzers/wiki/FL0012) from [Faithlife.Analyzers](https://github.com/Faithlife/FaithlifeAnalyzers), which reports when `Command` is called with an interpolated string.

```csharp
// Don't do this!
connector.Command(
    $"insert into widgets (name, height) values ({name}, {height});"
    ).Execute();
```

SQL text and parameters can be composed using instances of the [`Sql`](Faithlife.Data.SqlFormatting/Sql.md) class with `Sql.Format`. `Sql` instances in interpolated strings are used to build the SQL statement. (They are not converted into parameters like any other value would be.) `Sql.Format` returns a `Sql` instance, so `Sql.Format` can be composed with `Sql.Format` as needed.

```csharp
IReadOnlyList<WidgetDto> GetWidgets(DbConnector connector,
    double? minHeight = null, string[]? fields = null)
{
    var fieldsSql = fields is null ? Sql.Raw("*") : Sql.Join(", ", fields.Select(Sql.Name));
    var whereSql = minHeight is null ? Sql.Empty : Sql.Format($"where height >= {minHeight}");
    return connector.Command(Sql.Format($"select {fieldsSql} from widgets {whereSql};"))
        .Query<WidgetDto>();
}
```

To create `Sql` instances, use static members on the [`Sql`](Faithlife.Data.SqlFormatting/Sql.md) class:

* [`Sql.And`](Faithlife.Data.SqlFormatting/Sql/And.md) joins SQL fragments with the `AND` operator.
* [`Sql.Clauses`](Faithlife.Data.SqlFormatting/Sql/Clauses.md) joins SQL fragments with newlines.
* [`Sql.ColumnNames`](Faithlife.Data.SqlFormatting/Sql/ColumnNames.md) and [`Sql.ColumnNamesWhere`](Faithlife.Data.SqlFormatting/Sql/ColumnNamesWhere.md) generate a list of column names from a DTO for SELECT and INSERT statements.
* [`Sql.ColumnParams`](Faithlife.Data.SqlFormatting/Sql/ColumnParams.md) and [`Sql.ColumnParamsWhere`](Faithlife.Data.SqlFormatting/Sql/ColumnParamsWhere.md) generate a list of parameters from a DTO for an INSERT statement.
* [`Sql.Concat`](Faithlife.Data.SqlFormatting/Sql/Concat.md) (or [`operator +`](Faithlife.Data.SqlFormatting/Sql/op_Addition.md)) concatenates SQL fragments.
* [`Sql.DtoParamNames`](Faithlife.Data.SqlFormatting/Sql/DtoParamNames.md) and [`Sql.DtoParamNamesWhere`](Faithlife.Data.SqlFormatting/Sql/DtoParamNames.md) generate a list of named parameters for DTO properties.
* [`Sql.Empty`](Faithlife.Data.SqlFormatting/Sql/Empty.md) is an empty SQL fragment.
* [`Sql.GroupBy`](Faithlife.Data.SqlFormatting/Sql/GroupBy.md) creates SQL for a `GROUP BY` clause, omitting it if no columns are specified.
* [`Sql.Having`](Faithlife.Data.SqlFormatting/Sql/Having.md) creates SQL for a `HAVING` clause, omitting it if no condition is specified.
* [`Sql.Join`](Faithlife.Data.SqlFormatting/Sql/Join.md) joins SQL fragments with a separator.
* [`Sql.LikePrefixParam`](Faithlife.Data.SqlFormatting/Sql/LikePrefixParam.md) generates a parameter with a LIKE pattern for prefix matching.
* [`Sql.List`](Faithlife.Data.SqlFormatting/Sql/List.md) creates a comma-delimited list of SQL fragments.
* [`Sql.Name`](Faithlife.Data.SqlFormatting/Sql/Name.md) creates SQL that quotes the specified identifier.
* [`Sql.OrderBy`](Faithlife.Data.SqlFormatting/Sql/OrderBy.md) creates SQL for an `ORDER BY` clause, omitting it if no columns are specified.
* [`Sql.Param`](Faithlife.Data.SqlFormatting/Sql/Param.md) generates a parameter for the specified value. If the same `Sql` instance is used more than once by a command, the same SQL parameter is provided for each use.
* [`Sql.ParamList`](Faithlife.Data.SqlFormatting/Sql/ParamList.md) creates a comma-delimited list of parameters set to the specified values.
* [`Sql.ParamTuple`](Faithlife.Data.SqlFormatting/Sql/ParamTuple.md) creates a comma-delimited list of parameters set to the specified values, surrounded by parentheses.
* [`Sql.Raw`](Faithlife.Data.SqlFormatting/Sql/Raw.md) creates raw SQL from the specified string.
* [`Sql.Tuple`](Faithlife.Data.SqlFormatting/Sql/Tuple.md) creates a comma-delimited list of SQL fragments, surrounded by parentheses.
* [`Sql.Where`](Faithlife.Data.SqlFormatting/Sql/Where.md) creates SQL for a `WHERE` clause, omitting it if no condition is specified.

Since commands are commonly created with a single call to `Sql.Format`, the [`CommandFormat`](Faithlife.Data/DbConnector/CommandFormat.md) method can be used as shorthand.

```csharp
void InsertWidget(DbConnector connector, string name, double height) =>
    connector.CommandFormat(
        $"insert into widgets (name, height) values ({name}, {height});"
        ).Execute();
```

To generate lowercase SQL keywords, use [`SqlSyntax.WithLowercaseKeywords()`](Faithlife.Data.SqlFormatting/SqlSyntax/WithLowercaseKeywords.md).

### Collection parameters

Database providers do not typically support collections as parameter values, which makes it difficult to run queries that use the `IN` operator. To expand a collection into a set of numbered parameters, use `...` after the parameter name in the SQL and Faithlife.Data will make the necessary substitutions.

```csharp
connector.Command(
    "select id from widgets where name in (@names...);",
    ("names", new[] { "one", "two", "three" })).Execute();
```

This is equivalent to:

```csharp
connector.Command(
    "select id from widgets where name in (@names_0, @names_1, @names_2);",
    ("names_0", "one"), ("names_1", "two"), ("names_2", "three")).Execute();
```

This works with [formatted SQL](#formatted-sql) as well.

```csharp
var names = new[] { "one", "two", "three" };
connector.Command(Sql.Format(
    $"select id from widgets where name in ({names}...);"
    )).Execute();
```

**Important note:** If the collection is empty, an `InvalidOperationException` will be thrown, since omitting the parameter entirely may not be valid (or intended) SQL.

Alternatively, use [`Sql.ParamTuple`](Faithlife.Data.SqlFormatting/Sql/ParamTuple.md) or similar:

```csharp
var names = new[] { "one", "two", "three" };
connector.Command(Sql.Format(
    $"select id from widgets where name in {Sql.ParamTuple(names)};"
    )).Execute();
```

For more ways to specify query parameters, see [advanced parameters](#advanced-parameters) below.

## Single-record queries

If your query is for a single record, call [`QuerySingleAsync()`](Faithlife.Data/DbConnectorCommand/QuerySingleAsync.md), which throws an exception if the query returns multiple records, or [`QueryFirstAsync()`](Faithlife.Data/DbConnectorCommand/QueryFirstAsync.md), which does not check for additional records and therefore may be more efficient.

```csharp
double height = await connector.Command(
    "select height from widgets where name = @name;",
    ("name", "First")).QuerySingleAsync<double>();
```

If your single-record query might also return no records, call [`QuerySingleOrDefaultAsync()`](Faithlife.Data/DbConnectorCommand/QuerySingleOrDefaultAsync.md) or [`QueryFirstOrDefaultAsync()`](Faithlife.Data/DbConnectorCommand/QueryFirstOrDefaultAsync.md), which return `default(T)` if no records were found.

```csharp
double? height = await connector.Command(
    "select height from widgets where name = @name;",
    ("name", "First")).QueryFirstOrDefaultAsync<double?>();
```

As always, drop the `Async` suffix for the synchronous API.

## Multiple result sets

If your query has multiple result sets, all of the records from all of the result sets will be read and mapped to the same type.

If you want to map each result set to its own type, call [`QueryMultipleAsync()`](Faithlife.Data/DbConnectorCommand/QueryMultipleAsync.md) and then call [`ReadAsync()`](Faithlife.Data/DbConnectorResultSets/ReadAsync.md) for each result set.

```csharp
await using (var sets = await connector.Command(
    "select name from widgets; select height from widgets;").QueryMultipleAsync())
{
    IReadOnlyList<string> names = await sets.ReadAsync<string>();
    IReadOnlyList<double> heights = await sets.ReadAsync<double>();
    // ...
}
```

## Unbuffered queries

The `Query` and `Read` methods read all of the records into an `IReadOnlyList<T>`. Reading all of the data as quickly as possible is often best for performance, but you can read the records one at a time by calling [`Enumerate()`](Faithlife.Data/DbConnectorCommand/Enumerate.md) or [`EnumerateAsync()`](Faithlife.Data/DbConnectorCommand/EnumerateAsync.md) instead.

```csharp
var averageHeight = connector.Command("select height from widgets;")
    .Enumerate<double>().Average();
```

Note that `EnumerableAsync` returns an `IAsyncEnumerable<T>`. Use `await foreach` and/or [System.Linq.Async](https://www.nuget.org/packages/System.Linq.Async/) to enumerate the values.

```csharp
double? minHeight = null;
double? maxHeight = null;
await foreach (var height in connector.Command("select height from widgets;")
    .EnumerateAsync<double>())
{
    minHeight = minHeight is null ? height : Math.Min(minHeight.Value, height4);
    maxHeight = maxHeight is null ? height : Math.Max(maxHeight.Value, height4);
}
```

## Cached commands

Use `Cache()` to potentially improve performance when executing the same query with different parameter values. Cached commands can particularly help with SQLite, which typically doesn't have as much I/O overhead as other databases. The `IDbCommand` object is cached indefinitely with the `DbConnector` object, so avoid caching commands that will only be executed once.

```csharp
foreach (var (name, size) in widgets)
{
    connector.Command(Sql.Format(
        $"insert into widgets (name, size) values ({name}, {size});"
    )).Cache().Execute();
}
```

## Prepared commands

Use `Prepare()` to automatically call `Prepare` on the `IDbCommand` before it is executed. This can be particularly beneficial with the [Npgsql provider](https://www.npgsql.org/) for [PostgreSQL](https://www.postgresql.org/). Consider using `Cache()` as well, which caches the prepared command and then reuses it without preparing it again. Be sure to measure any performance advantage of caching and/or preparing commands; in particular, preparing the command may *hurt* performance in some scenarios.

```csharp
foreach (var (name, size) in widgets)
{
    connector.Command(Sql.Format(
        $"insert into widgets (name, size) values ({name}, {size});"
    )).Prepare().Cache().Execute();
}
```

## Command timeout

To override the default timeout for a particular command, use `WithTimeout()`.

```csharp
connector.Command(Sql.Format(
    $"insert into widgets (name, size) values ({name}, {size});"
)).WithTimeout(TimeSpan.FromMinutes(1)).Execute();
```

## Stored procedures

`DbConnector` also works with stored procedures. Simply call [`StoredProcedure()`](Faithlife.Data/DbConnector/StoredProcedure.md) instead of `Command()` and pass the name of the stored procedure instead of a SQL query.

```csharp
connector.StoredProcedure("CreateWidget", ("name", name), ("size", size)).Execute();
```

## Bulk insert

The [`BulkInsert()`](Faithlife.Data.BulkInsert/BulkInsertUtility/BulkInsert.md) and [`BulkInsertAsync()`](Faithlife.Data.BulkInsert/BulkInsertUtility/BulkInsertAsync.md) extension methods allow simple and efficient insertion of many rows into a database table.

The simplest way to insert many rows into a database table is to execute `INSERT` commands in a loop. Unfortunately, this can be extremely slow, even when the commands are all executed in a single transaction. (SQLite is a notable exception here; inserting one row at a time in a loop can be considerably *faster* than doing a bulk insert.)

Each DBMS has its own preferred approaches for efficiently inserting many rows into a database, but the most portable way is to execute an `INSERT` command with multiple rows in the `VALUES` clause, like so:

```sql
insert into widgets (name, size) values ('foo', 22), ('bar', 14), ('baz', 42)
```

Building a SQL statement for a large number of rows is straightforward, but runs the risk of SQL injection problems if the SQL isn't escaped propertly.

Using command parameters is safer, but building and executing the SQL is more complex. Furthermore, databases often have a limit on the maximum number of command parameters that can be used, so it can be necessary to execute multiple SQL statements, one for each batch of rows to insert.

`BulkInsert()` builds the SQL commands for each batch and injects the command parameters as needed.

```csharp
var widgets = new[]
{
    new { name = "foo", size = 22 },
    new { name = "bar", size = 14 },
    new { name = "baz", size = 42 },
};
connector.Command("insert into widgets (name, size) values (@name, @size)...")
    .BulkInsert(widgets.Select(DbParameters.FromDto));
```

The `...` after the `VALUES` clause must be included. It is used by `BulkInsert` to find the end of the `VALUES` clause that will be transformed. The call above will build a SQL statement like so:

```sql
insert into widgets (name, size) values (@name_0, @size_0), (@name_1, @size_1), (@name_2, @size_2)
```

The actual SQL statement will have as many parameters as needed to insert all of the specified rows. If the total number of command parameters would exceed 999 (a reasonable number for many databases), it will execute multiple SQL commands until all of the rows are inserted.

All of the transformed SQL will be executed for each batch, so including additional statements before or after the `INSERT` statement is not recommended.

Execute the method within a transaction if it is important to avoid inserting only some of the rows if there is an error.

The `BulkInsert()` and `BulkInsertAsync()` methods of the `BulkInsertUtility` static class are extension methods on [`DbConnectorCommand`](Faithlife.Data/DbConnectorCommand.md). They support an optional [`BulkInsertSettings`](Faithlife.Data.BulkInsert/BulkInsertSettings.md) parameter that allows you to change the maximum number of command parameters and/or the maximum number of rows per batch.

The method returns the total number of rows affected (or, more specifically, the sum of the row counts returned when executing the SQL commands for each batch).

You can also use [formatted SQL](#formatted-sql) to do bulk insertion more explicitly:

```csharp
var columnNamesSql = Sql.ColumnNames(widgets[0].GetType());
foreach (var chunk in widgets.Chunk(1000))
{
    connector.CommandFormat($@"
        insert into widgets ({columnNamesSql})
        values {Sql.Join(",", chunk.Select(x => Sql.Format($"({Sql.ColumnParams(x)})")))};"
        ").Execute();
}
```

## Advanced record mapping

This section documents additional scenarios for mapping records to values.

### object/dynamic

Record fields can be mapped to `object` or `dynamic`. If a single field is mapped to `object` or `dynamic`, the object from `IDataRecord.GetValue()` is returned directly.

```csharp
var heights = connector.Command("select height from widgets;")
    .Query<object>(); // returns boxed doubles
```

If multiple fields are mapped to `object` or `dynamic`, an [`ExpandoObject`](https://docs.microsoft.com/dotnet/api/system.dynamic.expandoobject) is returned where each property corresponds to the name and value of a mapped field.

```csharp
dynamic widget = connector.Command("select name, height from widgets;")
    .Query<dynamic>()[0];
string name = widget.name;
```

Unfortunately, `object` and `dynamic` cannot have different algorithms. The implementation cannot distinguish between them, because `typeof(T) == typeof(object)` when `T` is `dynamic`. To avoid confusion, use `object` when mapping a single field and `dynamic` when mapping multiple fields.

### Dictionary

Record fields can also be mapped to a dictionary of strings to objects, in which case each field gets a key/value pair in the dictionary. The supported dictionary types are `Dictionary<string, object>`, `IDictionary<string, object>`, `IReadOnlyDictionary<string, object>`, and `IDictionary`.

```csharp
var dictionary = connector.Command("select name, height from widgets;")
    .Query<Dictionary<string, object>>()[0];
double height = (double) dictionary["height"];
```

### Advanced tuples

Tuples can include multi-field types like DTOs and `dynamic`.

```csharp
IReadOnlyList<(WidgetDto Widget, long NameLength)> GetWidgetAndNumber(DbConnector connector) =>
    connector.Command("select id, height, length(name) from widgets;")
        .Query<(WidgetDto, long)>();
```

If the tuple has two or more multi-field types, all but the last must be terminated by a `null` record value whose name is `null`.

```csharp
IReadOnlyList<(WidgetDto Widget, dynamic Etc)> GetWidgetAndDynamic(DbConnector connector) =>
    connector.Command("select id, height, null, 1 as more, 2 as data from widgets;")
        .Query<(WidgetDto, dynamic)>();
```

### Mapping delegate

For full control over the mapping, the client can specify the `map` parameter, which is of type `Func<IDataRecord, T>`. That delegate will be called for each [`IDataRecord`](https://docs.microsoft.com/dotnet/api/system.data.idatarecord) instance returned by the query.

```csharp
IReadOnlyList<string> GetWidgetNames(DbConnector connector) =>
    connector.Command("select name from widgets;")
        .Query(x => x.GetString(0));
```

The [`DataRecordExtensions`](Faithlife.Data/DataRecordExtensions.md) static class provides [`Get<T>()`](Faithlife.Data/DataRecordExtensions/Get.md) extension methods on `IDataRecord` for mapping all or part of a record into the specified type.

```csharp
IReadOnlyList<double> GetWidgetHeights(DbConnector connector) =>
    connector.Command("select name, height from widgets;")
        .Query(x => x.Get<double>(1));
```

Fields can also be accessed by name, though that uses [`IDataRecord.GetOrdinal()`](https://docs.microsoft.com/dotnet/api/system.data.idatarecord.getordinal) and is thus slightly less efficient.

```csharp
IReadOnlyList<double> GetWidgetHeights(DbConnector connector) =>
    connector.Command("select name, height from widgets;")
        .Query(x => x.Get<double>("height"));
```

You can also read multiple fields.

```csharp
IReadOnlyList<(string Name, double Height)> GetWidgetInfo(DbConnector connector) =>
    connector.Command("select id, name, height from widgets;")
        .Query(x => x.Get<(string, double)>(index: 1, count: 2));
```

C# 8 range syntax can be used:

```csharp
IReadOnlyList<(string Name, double Height)> GetWidgetInfo(DbConnector connector) =>
    connector.Command("select id, name, height from widgets;")
        .Query(x => x.Get<(string, double)>(1..3));
```

You can also use the delegate to avoid the `null` terminator when reading two or more multi-field types. To avoid having to count fields, we can use a `Get<T>()` overload that takes a start name and an end name to specify the range.

```csharp
IReadOnlyList<(WidgetDto Widget, dynamic Etc)> GetWidgetAndDynamic2(DbConnector connector) =>
    connector.Command("select id, height, 1 as more, 2 as data from widgets;")
        .Query(x => (x.Get<WidgetDto>("id", "height"), x.Get<dynamic>("more", "data")));
```

## Advanced parameters

The [`DbParameters`](Faithlife.Data/DbParameters.md) structure can be used to build a list of parameters by calling one of the [`Create()`](Faithlife.Data/DbParameters/Create.md) methods.

You can add additional parameters by calling the [`Add()`](Faithlife.Data/DbParameters/Add.md) methods, but note that `DbParameters` is an *immutable* collection, so you will need to use the return value of the `Add` method.

```csharp
var tallWidgets = connector.Command(
    "select id from widgets where height >= @minHeight and height <= @maxHeight;",
    DbParameters.Create("minHeight", minHeight).Add("maxHeight", maxHeight)).Query<long>();
```

### Parameters from DTOs/collections

Use [`DbParameters.FromDto()`](Faithlife.Data/DbParameters/FromDto.md) or [`DbParameters.AddDto()`](Faithlife.Data/DbParameters/AddDto.md) to create parameters from the names and values of public properties and fields, e.g. of anonymous types.

```csharp
var newWidget = new WidgetDto { Name = "Third", Height = 1.414 };
connector.Command(
    "insert into widgets (name, height) values (@Name, @Height);",
    DbParameters.FromDto(newWidget)).Execute();

var tallWidgets = connector.Command(
    "select id from widgets where height >= @minHeight and height <= @maxHeight;",
    DbParameters.FromDto(new { minHeight = 1.0, maxHeight = 100.0 })).Query<long>();
```

Use [`DbParameters.FromDtoWhere()`](Faithlife.Data/DbParameters/FromDtoWhere.md) or [`DbParameters.AddDtoWhere()`](Faithlife.Data/DbParameters/AddDtoWhere.md) to create parameters from a subset of the public properites and fields.

Use [`DbParameters.FromDtos()`](Faithlife.Data/DbParameters/FromDtos.md) or [`DbParameters.AddDtos()`](Faithlife.Data/DbParameters/AddDtos.md) to create parameters for many DTOs at once.

Use [`DbParameters.FromMany()`](Faithlife.Data/DbParameters/FromMany.md) or [`DbParameters.AddMany()`](Faithlife.Data/DbParameters/AddMany.md) to explicitly create parameters from a collection.

### Provider-specific parameters

If the parameter value implements `IDbDataParameter`, that object is used as the parameter after setting its `ParameterName` property.

```csharp
double height = await connector.Command(
    "select height from widgets where name = @name;",
    ("name", new SqliteParameter { Value = "Bob", SqliteType = SqliteType.Text }))
    .QuerySingleAsync<double>();
```

## Enhancing the API

To avoid defining too many method overloads, [starting a command](Faithlife.Data/DbConnector/Command.md) and [executing a query](Faithlife.Data/DbConnectorCommand/Query.md) use chained methods. Feel free to reduce typing by creating your own extension methods that match your usage of the library.

For example, this extension method can be used to execute a query with parameters from a DTO in one method call:

```csharp
public static IReadOnlyList<T> Query<T>(this DbConnector connector,
    string sql, object param) =>
        connector.Command(sql, DbParameters.FromDto(param))
            .Query<T>();
```

### Use with Dapper

If you like the Dapper query API but want to use `DbConnector` to track the current transaction, use extension methods to call Dapper, accessing the [`Connection`](Faithlife.Data/DbConnector/Connection.md) and [`Transaction`](Faithlife.Data/DbConnector/Transaction.md) properties as needed.

```csharp
public static IEnumerable<T> Query<T>(this DbConnector connector,
    string sql, object param = null, bool buffered = true) =>
        connector.Connection
            .Query<T>(sql, param, connector.Transaction, buffered: buffered);
```
