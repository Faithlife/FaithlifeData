# Release Notes

## 1.22.0-beta.2

* Support new `Sql` methods: `And`, `Clauses`, `GroupBy`, `Having`, `List`, `Or`, `OrderBy`, `ParamList`, `ParamTuple`, `Tuple`, `Where`.
* Support lowercase keywords via `SqlSyntax`.

## 1.21.2

* Reuse the same parameter when an instance from `Sql.Param` is used more than once.

## 1.21.1

* Add .NET 6 target. Improve reference nullability.

## 1.21.0

* Add `OpenConnection` to `DbProviderMethods`. This allows the implementation of a more resilient connection opener.
* Introduce `DelegatingDbProviderMethods`.

## 1.20.1

* Use `DisposeAsync` when available.

## 1.20.0

* Support `DbConnector.ReleaseConnection`.

## 1.19.0

* Support `Sql.DtoParamNames` and `Sql.DtoParamNamesWhere`.
* Support `DbParameters.FromDtoWhere` and `DbParameters.AddDtoWhere`.

## 1.18.0

* Support `Sql.ColumnNamesWhere` and `Sql.ColumnParamsWhere`.

## 1.17.0

* Support `DbConnectorPool`.

## 1.16.0

* Support `DefaultIsolationLevel`.

## 1.15.0

* `Sql.ColumnNames` should support tuples of DTOs.

## 1.14.0

* Support snake case when generating column names for a DTO.

## 1.13.0

* Use `Sql.ColumnNames` to format a comma-delimited list of column names for a DTO.
* Use `Sql.ColumnParams` to format a comma-delimited list of parameters from a DTO.
* Respect `ColumnAttribute` on DTOs when querying or using `Sql.ColumnNames`.

## 1.12.0

* `connector.CommandFormat($"...")` is shorthand for `connector.Command(Sql.Format($"..."))`.

## 1.11.0

* Map strings to enumerated types.

## 1.10.0

* Use `Sql.Concat` or `operator +` to concatenate SQL fragments.

## 1.9.0

* Use `Sql.Empty` for empty SQL fragments.
* Use `Sql.Join` to join SQL fragments.
* Use `Sql.LikePrefixParam` to match a prefix with `LIKE`.
* Use `Sql.Name` to quote identifiers. Use with `SqlSyntax.MySql`, `SqlSyntax.Sqlite`, etc., for the proper quoting syntax.
* Add `SqlSyntax` to `DbConnector` and `DbConnectorSettings`.
* Add `DelegatingDbConnector` and `DelegatingSqlSyntax`.
* Drop support for format specifiers in formatted SQL. (Minor breaking change.)

## 1.8.0

* Reuse parameter objects in cached commands to maximize performance and meet the requirements of some ADO.NET providers for prepared commands.
* Bulk insert respects command timeout, caching, and preparing.
* Support attaching a native transaction.
* Prepare commands asynchronously.

## 1.7.0

* `Sql.Format` creates parameters via string interpolation.
* Support mapping data records to DTOs with read-only properties. (Uses [Faithlife.Reflection](https://github.com/Faithlife/FaithlifeReflection)).

## 1.6.0

* Create parameters from collections of values or DTOs.
* Customize parameter names when created from DTOs or collections.
* Support stored procedures.
* Support command timeout.
* Support prepared commands.
* Support mapping data records to C# 9 positional records.
* Thanks to [tywmick](https://github.com/tywmick) for contributing!

## 1.5.0

* Support reading a blob as a `Stream`.

## 1.4.1

* Fix error message on empty collection query parameter.

## 1.4.0

* Support cached commands.

## 1.3.2

* Add `BulkInsertAsync` overload.

## 1.3.1

* Add `BulkInsert` and `BulkInsertAsync` extension methods to `DbConnectorCommand`.
* Add `ToDictionary` to `DbParameters`.
* Add `Connector` `DbConnectorCommand`.

## 1.2.0

* Add single parameter `DbParameters.Create` like `DbParameters.Add`.
* Add special syntax for collection parameters.

## 1.1.0

* Create/add parameters from sequences of parameter tuples with non-`object` value types.
* Create/add parameters from dictionaries.
* Drop `MaybeNull` attribute from `QueryFirstOrDefault` and `QuerySingleOrDefault`.

## 1.0.0

* Official release.

## 0.300.1

* Try to avoid assembly binding errors from downstream dependencies.

## 0.300.0

* **Breaking:** Allow `object` to return a single field as-is instead of building a dynamic object with one property.
* **Breaking:** Make `ProviderMethods` property internal (otherwise it seems important to the API).
* **Breaking:** Use `ValueTask` instead of `Task`.
* **Breaking:** Rename `read` parameter to `map`.
* Ignore underscores when mapping fields to DTO properties.
* Support index/range and `IAsyncEnumerable` on .NET Standard 2.0.
* Support `IAsyncDisposable`.
* Add `Enumerate` to `DbConnectorResultSets`.
* Support mapping records to dictionaries.
* Add null record fields when mapping to dynamic.

## 0.200.0

* **Breaking:** Eliminate `DataRecordUtility`.
* **Breaking:** Rename `DbConnectorResultSet` to `DbConnectorResultSets`.
* **Breaking:** Make `DbParameters` an immutable `struct`.
* **Breaking:** Use `count` parameter name consistently.
* **Breaking:** Support nullable reference types. Make some previously nullable parameters and properties non-nullable.
* Add `QueryMultipleAsync`.
* Add `EnumerateAsync`, which uses the new `IAsyncEnumerable` (.NET Standard 2.1).
* Support index/range for data record access (.NET Standard 2.1).
* Expose text and parameters of commands.
* Support new async ADO.NET methods from .NET Standard 2.1.
* Add XML documentation comments.
* Improve NuGet package description and tags.

## 0.100.1

* Fix bug with QueryFirst/Single and multiple result sets.

## 0.100.0

* Advance version past internal version.

## 0.10.0

* Stop using CommandBehavior.SequentialAccess.

## 0.1.0

* Initial release.
