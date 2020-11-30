# Faithlife.Data

**Faithlife.Data** provides helpers for querying ADO.NET-compatible databases.

[![NuGet](https://img.shields.io/nuget/v/Faithlife.Data.svg)](https://www.nuget.org/packages/Faithlife.Data)

## Overview

The **Faithlife.Data** class library provides an enhanced API for interacting with ADO.NET-compatible databases. It is similar to [Dapper](https://github.com/StackExchange/Dapper) and other "micro" ORMs for .NET.

To use this library, call [`DbConnector.Create()`](Faithlife.Data/DbConnector/Create.md) to create a [`DbConnector`](Faithlife.Data/DbConnector.md) with a valid [`IDbConnection`](https://docs.microsoft.com/dotnet/api/system.data.idbconnection) from your favorite ADO.NET database provider, e.g. [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) for SQL Server or [MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) for MySQL.

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
* Expand [collection parameters](#parameters-from-collections) to a list of numbered parameters for easier `IN` support.
* Use [bulk insert](#bulk-insert) to easily and efficiently insert multiple rows into a table.

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
* Both Faithlife.Data and Dapper will edit the SQL when substituting a collection parameter for a list of dynamically named parameters. The syntax used by Faithlife.Data is more explicit, so scenarios where the SQL is edited are **more predictable**.

## Creating a connector

Like `IDbConnection`, `DbConnector` is *not* thread-safe, so you will need one instance per connection. Consider defining a method to easily create a connection to your database.

```csharp
DbConnector CreateConnector() =>
    DbConnector.Create(new SQLiteConnection("Data Source=:memory:"));
```

## Executing a command

Once you have a connector, you can open the connection with [`OpenConnection()`](Faithlife.Data/DbConnector/OpenConnection.md), create a command with [`Command()`](Faithlife.Data/DbConnector/Command.md), and execute the command with [`Execute()`](Faithlife.Data/DbConnectorCommand/Execute.md).

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

## Accessing the database asynchronously

Every method that has communicates with the database has an asynchronous equivalent, e.g. [`ExecuteAsync()`](Faithlife.Data/DbConnectorCommand/ExecuteAsync.md).

```csharp
using (var connector = CreateConnector())
using (await connector.OpenConnectionAsync())
{
    await connector.Command(@"
create table widgets (
  id integer primary key autoincrement,
  name text not null);").ExecuteAsync();
}
```

Note that asynchronous methods in this library return `ValueTask`, not `Task`, so be sure to [follow the relevant guidelines](https://docs.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1), e.g. don't `await` a `ValueTask` more than once.

## Automatically opening connections

Calling `OpenConnection` every time you use a connector can get tiresome. You could move the `OpenConnection` call to your connector creation method, or you can use the [`AutoOpen`](Faithlife.Data/DbConnectorSettings/AutoOpen.md) setting.

```csharp
DbConnector OpenConnector() => DbConnector.Create(
    new SQLiteConnection("Data Source=:memory:"),
    new DbConnectorSettings { AutoOpen = true });
```

If you want to wait to actually open the database connection until just before it is first used, also set the [`LazyOpen`](Faithlife.Data/DbConnectorSettings/LazyOpen.md) setting.

## Using transactions

To leverage a database transaction, call [`BeginTransaction()`](Faithlife.Data/DbConnector/BeginTransaction.md) before executing any commands, and then call [`CommitTransaction()`](Faithlife.Data/DbConnector/CommitTransaction.md) before disposing the return value of `BeginTransaction()`.

```csharp
using (var connector = OpenConnector())
using (await connector.BeginTransactionAsync())
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

If `CommitTransaction()` is not called, the transaction will be rolled back when the return value of `BeginTransaction()` is disposed.

ADO.NET requres that the `Transaction` property of [`IDbCommand`](https://docs.microsoft.com/dotnet/api/system.data.idbcommand) be set to the current transaction; `DbConnector` takes care of that automatically when executing commands.

## Mapping database records

The querying methods of this class library (e.g. [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md)) are generic, allowing the caller to specify the type to which each database record should be mapped.

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

### Simple types

Simple types must match the type returned from [`IDataRecord.GetValue()`](https://docs.microsoft.com/dotnet/api/system.data.idatarecord.getvalue) exactly. No conversions are performed, except between nullable and non-nullable types. Be sure to use a nullable type if the value could be null, e.g. `int?`; an exception will be thrown if the field is null but the type is not nullable.

The supported simple types are `string`, `long`, `int`, `short`, `byte`, `ulong`, `uint`, `ushort`, `sbyte`, `double`, `float`, `decimal`, `bool`, `Guid`, `DateTime`, `DateTimeOffset`, and `TimeSpan`.

Use `byte[]` or `Stream` to return the bytes from a "blob" column. If a non-null `Stream` is returned, be sure to dispose it.

### Tuples

Use tuples to map multiple record fields at once. Each tuple item is read from the record in order; the tuple item names, if any, are ignored.

```csharp
IReadOnlyList<(string Name, double Height)> GetWidgetInfo(DbConnector connector) =>
    connector.Command("select name, height from widgets;")
        .Query(x => x.Get<(string, double)>(index: 0, count: 2));
```

C# 8 range syntax can also be used:

```csharp
IReadOnlyList<(string Name, double Height)> GetWidgetInfo(DbConnector connector) =>
    connector.Command("select name, height from widgets;")
        .Query(x => x.Get<(string, double)>(0..2));
```

If every field of the record is being mapped, the field range can be omitted altogether:

```csharp
IReadOnlyList<(string Name, double Height)> GetWidgetInfo(DbConnector connector) =>
    connector.Command("select name, height from widgets;")
        .Query(x => x.Get<(string, double)>());
```

And since callers usually want to map every field, that is the behavior of the method if the `map` parameter is omitted:

```csharp
IReadOnlyList<(string Name, double Height)> GetWidgetInfo(DbConnector connector) =>
    connector.Command("select name, height from widgets;")
        .Query<(string, double)>();
```

### DTOs

If the library doesn't recognize a type, it is assumed to be a variable-field DTO (data transfer object) type, i.e. a type with a default constructor and one or more read/write properties.

```csharp
class WidgetDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public double Height { get; set; }
}
```

When a DTO type is used, a new instance of the DTO is created, and each record field is mapped to a DTO property whose name matches the field name, ignoring case and any underscores (so `full_name` would map successfully to `FullName`, for example). Not every property of the DTO must be used, but every mapped field must have a corresponding property.

```csharp
IReadOnlyList<WidgetDto> GetWidgets(DbConnector connector) =>
    connector.Command("select id, name, height from widgets;").Query<WidgetDto>();
```

### object/dynamic

Record fields can also be mapped to `object` or `dynamic`. If a single field is mapped to `object` or `dynamic`, the object from `IDataRecord.GetValue()` is returned directly.

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

Unfortunately, `object` and `dynamic` cannot have different algorithms. The implementation cannot distinguish between them, because `typeof(T) == typeof(object)` either way. To avoid confusion, use `object` when mapping a single field and `dynamic` when mapping multiple fields.

### Dictionary

Record fields can also be mapped to a dictionary of strings to objects, in which case each field gets a key/value pair in the dictionary. The supported dictionary types are `Dictionary<string, object>`, `IDictionary<string, object>`, `IReadOnlyDictionary<string, object>`, and `IDictionary`.

```csharp
var dictionary = connector.Command("select name, height from widgets;")
    .Query<Dictionary<string, object>>();
double height = (double) dictionary["height"];
```

### Advanced tuples

Tuples can also include variable-field types like DTOs and `dynamic`. (Sorry, the examples are getting weird, but these features are useful with more interesting database schemas and queries.)

```csharp
IReadOnlyList<(WidgetDto Widget, long NameLength)> GetWidgetAndNumber(DbConnector connector) =>
    connector.Command("select id, height, length(name) from widgets;")
        .Query<(WidgetDto, long)>();
```

If the tuple has two or more variable-field types, all but the last must be terminated by a `null` record value whose name is `null`.

```csharp
IReadOnlyList<(WidgetDto Widget, dynamic Etc)> GetWidgetAndDynamic(DbConnector connector) =>
    connector.Command("select id, height, null, 1 as more, 2 as data from widgets;")
        .Query<(WidgetDto, dynamic)>();
```

If you don't like that strategy, you can always use an explicit `map` parameter. To avoid having to count fields, we use a `Get<T>()` overload that takes a start name and an end name to specify the range.

```csharp
IReadOnlyList<(WidgetDto Widget, dynamic Etc)> GetWidgetAndDynamic2(DbConnector connector) =>
    connector.Command("select id, height, 1 as more, 2 as data from widgets;")
        .Query(x => (x.Get<WidgetDto>("id", "height"), x.Get<dynamic>("more", "data")));
```

## Parameterized queries

When executing parameterized queries, the parameter values are specified with the [`DbConnector.Command()`](Faithlife.Data/DbConnector/Command.md) method. The simplest way to specify command parameters is via one or more string/object tuples after the command SQL.

```csharp
var tallWidgets = connector.Command(
    "select id from widgets where height >= @minHeight and height <= @maxHeight;",
    ("minHeight", 1.0), ("maxHeight", 100.0)).Query<long>();
```

The [`DbParameters`](Faithlife.Data/DbParameters.md) structure can be used to build a list of parameters by calling one of the [`Create()`](Faithlife.Data/DbParameters/Create.md) methods.

You can add additional parameters by calling the [`Add()`](Faithlife.Data/DbParameters/Add.md) methods, but note that `DbParameters` is an *immutable* collection, so you will need to use the return value of the `Add` method.

```csharp
var tallWidgets = connector.Command(
    "select id from widgets where height >= @minHeight and height <= @maxHeight;",
    DbParameters.Create("minHeight", 1.0).Add("maxHeight", 100.0)).Query<long>();
```

### Parameters from DTOs

Use [`DbParameters.FromDto()`](Faithlife.Data/DbParameters/FromDto.md) or [`DbParameters.AddDto()`](Faithlife.Data/DbParameters/AddDto.md) to create parameters from the names and values of public properties and fields, e.g. of anonymous types.

```csharp
var newWidget = new WidgetDto { Name = "Third", Height = 1.414 };
connector.Command(
    "insert into widgets (name, height) values (@Name, @Height);",
    DbParameters.FromDto(newWidget)).Execute();

var tallWidgets = connector.Command(
    "select id from widgets where height >= @minHeight and height <= @maxHeight;",
    DbParameters.FromDto(new { minHeight = 1.0, maxHeight - 100.0 })).Query<long>();
```

### Parameters from collections

Database providers do not typically support collections as parameter values, which makes it difficult to run queries that use the `IN` operator. To expand a collection into a set of numbered parameters, use `...` after the parameter name in the SQL and Faithlife.Data will make the necessary substitutions.

```csharp
var newWidget = new WidgetDto { Name = "Third", Height = 1.414 };
connector.Command(
    "select id from widgets where name in (@names...);",
    ("names", new[] { "one", "two", "three" })).Execute();
```

This is equivalent to:

```csharp
var newWidget = new WidgetDto { Name = "Third", Height = 1.414 };
connector.Command(
    "select id from widgets where name in (@names_0, @names_1, @names_2);",
    ("names_0", "one"), ("names_1", "two"), ("names_2", "three")).Execute();
```

**Important note:** If the collection is empty, an `InvalidOperationException` will be thrown, since omitting the parameter entirely may not be valid (or intended) SQL.

### Formatting SQL

Typing parameter names in the SQL command text and parameters objects can sometimes be redundant. [`FormattableString`](https://docs.microsoft.com/en-us/dotnet/api/system.formattablestring) can be used
to put the parameters in the SQL safely by using [`Sql.Format()`](Faithlife.Data.SqlFormatting/Sql/Format.md) to format a `FormattableString` into command text and parameters.

```csharp
var name = "First";
var height = 6.875;
connector.Command(Sql.Format(
    $"insert into widgets (name, height) values ({name}, {height});"
    )).Execute();
```

This is equivalent to:

```csharp
var name = "First";
var height = 6.875;
connector.Command(
    "insert into widgets (name, height) values (@param0, @param1);",
    ("param0", name), ("param1", height)).Execute();
```

Note that using `FormattableString` without `Sql.Format()` is still a SQL injection vulnerability:

```csharp
// Don't do this!
connector.Command(
    $"insert into widgets (name, height) values ({name}, {height});"
    ).Execute();
```


SQL text and paramters can be composed using the [`Sql`](Faithlife.Data.SqlFormatting/Sql.md) class. Non-parameterized SQL can be expressed using [`Sql.Raw()`](Faithlife.Data.SqlFormatting/Sql/Raw.md) or the `raw` format specifier.

```csharp
IReadOnlyList<WidgetDto> GetWidgets(DbConnector connector, double? minHeight = default)
{
    var whereSql = minHeight is null ? Sql.Raw("") : Sql.Format($"where height >= {minHeight}");
    return connector.Command(Sql.Format($"select * from widgets {whereSql};"))
        .Query<WidgetDto>();
}
```

```csharp
IReadOnlyList<WidgetDto> GetWidgets(DbConnector connector, string[]? fields = default)
{
    var fieldsFragment = fields is null ? "*" : string.Join(", ", fields);
    return connector.Command(Sql.Format($"select {fieldsFragment:raw} from widgets;"))
        .Query<WidgetDto>();
}
```

## Single-record queries

If your query is for a single record, call [`QuerySingle()`](Faithlife.Data/DbConnectorCommand/QuerySingle.md), which throws an exception if the query returns multiple records, or [`QueryFirst()`](Faithlife.Data/DbConnectorCommand/QueryFirst.md), which does not check for additional records and therefore may be more efficient.

```csharp
double height = connector.Command(
    "select height from widgets where name = @name;",
    ("name", "First")).QuerySingle<double>();
```

If your single-record query might also return no records, call [`QuerySingleOrDefault()`](Faithlife.Data/DbConnectorCommand/QuerySingleOrDefault.md) or [`QueryFirstOrDefault()`](Faithlife.Data/DbConnectorCommand/QueryFirstOrDefault.md), which return `default(T)` if no records were found.

```csharp
double? height = connector.Command(
    "select height from widgets where name = @name;",
    ("name", "First")).QueryFirstOrDefault<double?>();
```

## Multiple result sets

If your query has multiple result sets, all of the records from all of the result sets will be read and mapped to the same type.

If you want to map each result set to its own type, call [`QueryMultiple()`](Faithlife.Data/DbConnectorCommand/QueryMultiple.md) and then call [`Read()`](Faithlife.Data/DbConnectorResultSets/Read.md) for each result set.

```csharp
using (var sets = connector.Command(
    "select name from widgets; select height from widgets;").QueryMultiple())
{
    names = sets.Read<string>();
    heights = sets.Read<double>();
}
```

## Unbuffered queries

The `Query` and `Read` methods read all of the records into an `IReadOnlyList<T>`. Reading all of the data as quickly as possible is often best for performance, but you can read the records one at a time by calling [`Enumerate()`](Faithlife.Data/DbConnectorCommand/Enumerate.md) instead.

```csharp
var averageHeight = connector.Command("select height from widgets;")
    .Enumerate<double>().Average();
```

## Bulk insert

The [`BulkInsert()`](Faithlife.Data.BulkInsert/BulkInsertUtility/BulkInsert.md) and [`BulkInsertAsync()`](Faithlife.Data.BulkInsert/BulkInsertUtility/BulkInsertAsync.md) extension methods allow simple and efficient insertion of many rows into a database table.

The simplest way to insert many rows into a database table is to execute `INSERT` commands in a loop. Unfortunately, this can be extremely slow, even when the commands are all executed in a single transaction.

Each DBMS has its own preferred approaches for efficiently inserting many rows into a database, but the most portable way is to execute an `INSERT` command with multiple rows in the `VALUES` clause, like so:

```
INSERT INTO widgets (name, size) VALUES ('foo', 22), ('bar', 14), ('baz', 42)
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
connector.Command("INSERT INTO widgets (name, size) VALUES (@name, @size)...")
    .BulkInsert(widgets.Select(DbParameters.FromDto));
```

The `...` after the `VALUES` clause must be included. It is used by `BulkInsert` to find the end of the `VALUES` clause that will be transformed. The call above will build a SQL statement like so:

```
INSERT INTO widgets (name, size) VALUES (@name_0, @size_0), (@name_1, @size_1), (@name_2, @size_2)
```

The actual SQL statement will have as many parameters as needed to insert all of the specified rows. If the total number of command parameters would exceed 999 (a reasonable number for many databases), it will execute multiple SQL commands until all of the rows are inserted.

All of the transformed SQL will be executed for each batch, so including additional statements before or after the `INSERT` statement is not recommended.

Execute the method within a transaction if it is important to avoid inserting only some of the rows if there is an error.

The `BulkInsert()` and `BulkInsertAsync()` methods of the `BulkInsertUtility` static class are extension methods on [`DbConnectorCommand`](Faithlife.Data/DbConnectorCommand.md). They support an optional [`BulkInsertSettings`](Faithlife.Data.BulkInsert/BulkInsertSettings.md) parameter that allows you to change the maximum number of command parameters and/or the maximum number of rows per batch.

The method returns the total number of rows affected (or, more specifically, the sum of the row counts returned when executing the SQL commands for each batch).

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
