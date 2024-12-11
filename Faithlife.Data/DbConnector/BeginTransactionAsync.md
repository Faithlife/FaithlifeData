# DbConnector.BeginTransactionAsync method (1 of 2)

Begins a transaction.

```csharp
public abstract ValueTask<DbTransactionDisposer> BeginTransactionAsync(
    CancellationToken cancellationToken = default)
```

| parameter | description |
| --- | --- |
| cancellationToken | The cancellation token. |

## Return Value

An IDisposable that should be disposed when the transaction has been committed or should be rolled back.

## See Also

* method [BeginTransaction](./BeginTransaction.md)
* class [DbTransactionDisposer](../DbTransactionDisposer.md)
* class [DbConnector](../DbConnector.md)
* namespace [Faithlife.Data](../../Faithlife.Data.md)

---

# DbConnector.BeginTransactionAsync method (2 of 2)

Begins a transaction.

```csharp
public abstract ValueTask<DbTransactionDisposer> BeginTransactionAsync(
    IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
```

| parameter | description |
| --- | --- |
| isolationLevel | The isolation level. |
| cancellationToken | The cancellation token. |

## Return Value

An IDisposable that should be disposed when the transaction has been committed or should be rolled back.

## See Also

* method [BeginTransaction](./BeginTransaction.md)
* class [DbTransactionDisposer](../DbTransactionDisposer.md)
* class [DbConnector](../DbConnector.md)
* namespace [Faithlife.Data](../../Faithlife.Data.md)

<!-- DO NOT EDIT: generated by xmldocmd for Faithlife.Data.dll -->