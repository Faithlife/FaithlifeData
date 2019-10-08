# Faithlife.Data

**Faithlife.Data** provides helpers for querying ADO.NET-compatible databases.

[![NuGet](https://img.shields.io/nuget/v/Faithlife.Data.svg)](https://www.nuget.org/packages/Faithlife.Data)

## Overview

The **Faithlife.Data** class library provides an enhanced API for interacting with ADO.NET-compatible databases. It is similar to [Dapper](https://github.com/StackExchange/Dapper) and other "micro" ORMs for .NET.

To use this library, call [`DbConnector.Create()`](Faithlife.Data/DbConnector/Create.md) to create a [`DbConnector`](Faithlife.Data/DbConnector.md) with a valid [`IDbConnection`](https://docs.microsoft.com/dotnet/api/system.data.idbconnection) from your favorite ADO.NET database provider, e.g. [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) for SQL Server or [MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) for MySQL.

With a `DbConnector`, you can:

* Automatically open the connection, via [`DbConnectorSettings.AutoOpen`](Faithlife.Data/DbConnectorSettings/AutoOpen.md).
* Wait to open the connection until it is needed, via [`DbConnectorSettings.LazyOpen`](Faithlife.Data/DbConnectorSettings/LazyOpen.md).
* Begin, commit, and rollback transactions, via [`DbConnector.BeginTransaction()`](Faithlife.Data/DbConnector/BeginTransaction.md), [`DbConnector.CommitTransaction()`](Faithlife.Data/DbConnector/CommitTransaction.md), etc.
* Create and execute database commands, automatically setting the transaction as needed, via [`DbConnector.Command()`](Faithlife.Data/DbConnector/Command.md) followed by [`Execute()`](Faithlife.Data/DbConnectorCommand/Execute.md), [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md), etc.
* Provide named command parameters from name/value tuples and/or DTO properties, via [`DbParameters`](Faithlife.Data/DbParameters.md).
* Efficiently map database records into simple data types, DTOs, and/or tuples ([details below](#mapping-database-records)).
* Read all records at once (via [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md)), or read records one at a time (via [`Enumerate()`](Faithlife.Data/DbConnectorCommand/Enumerate.md)).
* Efficiently access only the first record of the query result, via [`QueryFirst()`](Faithlife.Data/DbConnectorCommand/QueryFirst.md), [`QuerySingleOrDefault()`](Faithlife.Data/DbConnectorCommand/QuerySingleOrDefault.md), etc.
* Access the database synchronously or asynchronously with cancellation support, e.g. [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md) vs. [`QueryAsync()`](Faithlife.Data/DbConnectorCommand/QueryAsync.md).
* Read multiple result sets from multi-statement commands, via [`QueryMultiple()`](Faithlife.Data/DbConnectorCommand/QueryMultiple.md).

Consult the [reference documentation](Faithlife.Data.md) for additional details.

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
    connector.Command(@"create table widgets (
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
    await connector.Command(@"create table widgets (
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

Fields can also be accessed by name, though that uses [`IDataRecord.GetOrdinal`](https://docs.microsoft.com/dotnet/api/system.data.idatarecord.getordinal) and is thus slightly less efficient.

```csharp
IReadOnlyList<double> GetWidgetHeights(DbConnector connector) =>
    connector.Command("select name, height from widgets;")
        .Query(x => x.Get<double>("height"));
```

### Simple types

Simple types must match the type returned from [`IDataRecord.GetValue`](https://docs.microsoft.com/dotnet/api/system.data.idatarecord.getvalue) exactly. No conversions are performed, except between nullable and non-nullable types. Be sure to use a nullable type if the value could be null, e.g. `int?`; an exception will be thrown if the field is null but the type is not nullable.

The supported simple types are `string`, `long`, `int`, `short`, `byte`, `ulong`, `uint`, `ushort`, `sbyte`, `double`, `float`, `decimal`, `bool`, `Guid`, `DateTime`, `DateTimeOffset`, and `TimeSpan`.

Use `byte[]` to return the bytes from a "blob" column.

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
    "select id from widgets where height >= @minHeight;",
    ("minHeight", 1.0)).Query<long>();
```

The `DbParameters` class can be used to build lists of parameters. `DbParameters.FromDto` creates parameters from the names and values of public properties and fields, e.g. of anonymous types.

```csharp
var newWidget = new WidgetDto { Name = "Third", Height = 1.414 };
connector.Command(
    "insert into widgets (name, height) values (@Name, @Height);",
    DbParameters.FromDto(newWidget)).Execute();

var tallWidgets = connector.Command(
    "select id from widgets where height >= @minHeight;",
    DbParameters.FromDto(new { minHeight = 1.0 })).Query<long>();
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

```
using (var sets = connector.Command(
    "select name from widgets; select height from widgets;").QueryMultiple())
{
    names = sets.Read<string>();
    heights = sets.Read<double>();
}
```

## Unbuffered queries

The `Query` and `Read` methods read all of the records into an `IReadOnlyList<T>`. Reading all of the data as quickly as possible is often best for performance, but you can read the records one at a time by calling [`Enumerate()`](Faithlife.Data/DbConnectorCommand/Enumerate.md) instead.

```
var averageHeight = connector.Command("select height from widgets;")
    .Enumerate<double>().Average();
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
