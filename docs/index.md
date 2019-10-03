# Faithlife.Data

**Faithlife.Data** provides helpers for querying ADO.NET-compatible databases.

[![NuGet](https://img.shields.io/nuget/v/Faithlife.Data.svg)](https://www.nuget.org/packages/Faithlife.Data)

## Overview

The **Faithlife.Data** class library provides an enhanced API for interacting with ADO.NET-compatible databases. It is similar to [Dapper](https://github.com/StackExchange/Dapper) and other "micro" ORMs for .NET.

To use this library, call [`DbConnector.Create()`](Faithlife.Data/DbConnector/Create.md) to create a [`DbConnector`](Faithlife.Data/DbConnector.md) with a valid `IDbConnection` from your favorite ADO.NET database provider, e.g. [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) for SQL Server or [MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) for MySQL.

With a `DbConnector`, you can:

* Automatically open the connection, via [`DbConnectorSettings.AutoOpen`](Faithlife.Data/DbConnectorSettings/AutoOpen.md).
* Wait to open the connection until it is needed, via [`DbConnectorSettings.LazyOpen`](Faithlife.Data/DbConnectorSettings/LazyOpen.md).
* Begin, commit, and rollback transactions, via [`DbConnector.BeginTransaction()`](Faithlife.Data/DbConnector/BeginTransaction.md), etc.
* Create and execute database commands, automatically setting the transaction as needed, via [`DbConnector.Command()`](Faithlife.Data/DbConnector/Command.md) followed by [`Execute()`](Faithlife.Data/DbConnectorCommand/Execute.md), [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md), etc.
* Provide named command parameters from name/value tuples and/or DTO properties, via [`DbParameters`](Faithlife.Data/DbParameters.md).
* Efficiently map database records into simple data types or DTOs.
* Split database records into tuples of simple data types and/or DTOs.
* Map database records into anything with a custom lambda expression.
* Read all records at once (via [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md)), or read records one at a time (via [`Enumerate()`](Faithlife.Data/DbConnectorCommand/Enumerate.md)).
* Efficiently access only the first record of the query result, via [`QueryFirst()`](Faithlife.Data/DbConnectorCommand/QueryFirst.md), [`QuerySingleOrDefault()`](Faithlife.Data/DbConnectorCommand/QuerySingleOrDefault.md), etc.
* Access the database synchronously or asynchronously with cancellation support, e.g. [`Query()`](Faithlife.Data/DbConnectorCommand/Query.md) vs. [`QueryAsync()`](Faithlife.Data/DbConnectorCommand/QueryAsync.md).
* Read multiple result sets from multi-statement commands, via [`QueryMultiple()`](Faithlife.Data/DbConnectorCommand/QueryMultiple.md).

Consult the [reference documentation](Faithlife.Data.md) for additional details.
