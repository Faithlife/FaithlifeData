# Version History

## Pending

Describe changes here when they're committed to the `master` branch. Move them to **Released** when the project version number is updated in preparation for publishing an updated NuGet package.

Prefix the description of the change with `[major]`, `[minor]`, or `[patch]` in accordance with [Semantic Versioning](https://semver.org/).

## Released

### 0.300.0

* [major] Allow `object` to return a single field as-is instead of building a dynamic object with one property.
* [minor] Ignore underscores when mapping fields to DTO properties.
* [minor] Support index/range and `IAsyncEnumerable` on .NET Standard 2.0.
* [minor] Support `IAsyncDisposable`.
* [major] Make `ProviderMethods` property internal (otherwise it seems important to the API).
* [major] Use `ValueTask` instead of `Task`.
* [major] Rename `read` parameter to `map`.
* [minor] Add `Enumerate` to `DbConnectorResultSets`.
* [minor] Support mapping records to dictionaries.
* [patch] Add null record fields when mapping to dynamic.

### 0.200.0

* [major] Eliminate `DataRecordUtility`.
* [major] Rename `DbConnectorResultSet` to `DbConnectorResultSets`.
* [major] Make `DbParameters` an immutable `struct`.
* [major] Use `count` parameter name consistently.
* [major] Support nullable reference types. Make some previously nullable parameters and properties non-nullable.
* [minor] Add `QueryMultipleAsync`.
* [minor] Add `EnumerateAsync`, which uses the new `IAsyncEnumerable` (.NET Standard 2.1).
* [minor] Support index/range for data record access (.NET Standard 2.1).
* [minor] Expose text and parameters of commands.
* [patch] Support new async ADO.NET methods from .NET Standard 2.1.
* [patch] Add XML documentation comments.
* [patch] Improve NuGet package description and tags.

### 0.100.1

* Fix bug with QueryFirst/Single and multiple result sets.

### 0.100.0

* Advance version past internal version.

### 0.10.0

* Stop using CommandBehavior.SequentialAccess.

### 0.1.0

* Initial release.
