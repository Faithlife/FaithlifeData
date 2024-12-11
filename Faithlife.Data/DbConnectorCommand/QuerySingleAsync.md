# DbConnectorCommand.QuerySingleAsync&lt;T&gt; method (1 of 2)

Executes the query, converting the first record to the specified type.

```csharp
public ValueTask<T> QuerySingleAsync<T>(CancellationToken cancellationToken = default)
```

## Remarks

Throws InvalidOperationException if no records are returned, or if more than one record is returned.

## See Also

* method [QuerySingle&lt;T&gt;](./QuerySingle.md)
* struct [DbConnectorCommand](../DbConnectorCommand.md)
* namespace [Faithlife.Data](../../Faithlife.Data.md)

---

# DbConnectorCommand.QuerySingleAsync&lt;T&gt; method (2 of 2)

Executes the query, converting the first record to the specified type with the specified delegate.

```csharp
public ValueTask<T> QuerySingleAsync<T>(Func<IDataRecord, T> map, 
    CancellationToken cancellationToken = default)
```

## Remarks

Throws InvalidOperationException if no records are returned, or if more than one record is returned.

## See Also

* method [QuerySingle&lt;T&gt;](./QuerySingle.md)
* struct [DbConnectorCommand](../DbConnectorCommand.md)
* namespace [Faithlife.Data](../../Faithlife.Data.md)

<!-- DO NOT EDIT: generated by xmldocmd for Faithlife.Data.dll -->