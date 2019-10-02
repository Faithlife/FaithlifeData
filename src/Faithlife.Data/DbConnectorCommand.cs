using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
#if NETSTANDARD2_1
using System.Runtime.CompilerServices;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	/// <summary>
	/// Encapsulates the text and parameters of a database command.
	/// </summary>
	public readonly struct DbConnectorCommand
	{
		/// <summary>
		/// The text of the command.
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// The parameters of the command.
		/// </summary>
		public DbParameters Parameters { get; }

		/// <summary>
		/// Executes the command, returning the number of rows affected.
		/// </summary>
		public int Execute()
		{
			using var command = Create();
			return command.ExecuteNonQuery();
		}

		/// <summary>
		/// Executes the command, returning the number of rows affected.
		/// </summary>
		public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
		{
			using var command = await CreateAsync(cancellationToken).ConfigureAwait(false);
			return await m_connector.ProviderMethods.ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Executes the query, reading every record and converting it to the specified type.
		/// </summary>
		public IReadOnlyList<T> Query<T>() =>
			DoQuery<T>(null);

		/// <summary>
		/// Executes the query, reading every record and converting it to the specified type with the specified delegate.
		/// </summary>
		public IReadOnlyList<T> Query<T>(Func<IDataRecord, T> read) =>
			DoQuery(read ?? throw new ArgumentNullException(nameof(read)));

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
		public T QueryFirst<T>() =>
			DoQueryFirst<T>(null, single: false, orDefault: false);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
		public T QueryFirst<T>(Func<IDataRecord, T> read) =>
			DoQueryFirst(read ?? throw new ArgumentNullException(nameof(read)), single: false, orDefault: false);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
		[return: MaybeNull]
		public T QueryFirstOrDefault<T>() =>
			DoQueryFirst<T>(null, single: false, orDefault: true);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
		[return: MaybeNull]
		public T QueryFirstOrDefault<T>(Func<IDataRecord, T> read) =>
			DoQueryFirst(read ?? throw new ArgumentNullException(nameof(read)), single: false, orDefault: true);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
		public T QuerySingle<T>() =>
			DoQueryFirst<T>(null, single: true, orDefault: false);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
		public T QuerySingle<T>(Func<IDataRecord, T> read) =>
			DoQueryFirst(read ?? throw new ArgumentNullException(nameof(read)), single: true, orDefault: false);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.
		/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
		[return: MaybeNull]
		public T QuerySingleOrDefault<T>() =>
			DoQueryFirst<T>(null, single: true, orDefault: true);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.
		/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
		[return: MaybeNull]
		public T QuerySingleOrDefault<T>(Func<IDataRecord, T> read) =>
			DoQueryFirst(read ?? throw new ArgumentNullException(nameof(read)), single: true, orDefault: true);

		/// <summary>
		/// Executes the query, converting each record to the specified type.
		/// </summary>
		public Task<IReadOnlyList<T>> QueryAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryAsync<T>(null, cancellationToken);

		/// <summary>
		/// Executes the query, converting each record to the specified type with the specified delegate.
		/// </summary>
		public Task<IReadOnlyList<T>> QueryAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			DoQueryAsync(read ?? throw new ArgumentNullException(nameof(read)), cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
		public Task<T> QueryFirstAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync<T>(null, single: false, orDefault: false, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
		public Task<T> QueryFirstAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync(read ?? throw new ArgumentNullException(nameof(read)), single: false, orDefault: false, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
		public Task<T> QueryFirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync<T>(null, single: false, orDefault: true, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
		public Task<T> QueryFirstOrDefaultAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync(read ?? throw new ArgumentNullException(nameof(read)), single: false, orDefault: true, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
		public Task<T> QuerySingleAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync<T>(null, single: true, orDefault: false, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
		public Task<T> QuerySingleAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync(read ?? throw new ArgumentNullException(nameof(read)), single: true, orDefault: false, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.
		/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
		public Task<T> QuerySingleOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync<T>(null, single: true, orDefault: true, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.
		/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
		public Task<T> QuerySingleOrDefaultAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync(read ?? throw new ArgumentNullException(nameof(read)), single: true, orDefault: true, cancellationToken);

		/// <summary>
		/// Executes the query, reading one record at a time and converting it to the specified type.
		/// </summary>
		public IEnumerable<T> Enumerate<T>() =>
			DoEnumerate<T>(null);

		/// <summary>
		/// Executes the query, reading one record at a time and converting it to the specified type with the specified delegate.
		/// </summary>
		public IEnumerable<T> Enumerate<T>(Func<IDataRecord, T> read) =>
			DoEnumerate(read ?? throw new ArgumentNullException(nameof(read)));

#if NETSTANDARD2_1
		/// <summary>
		/// Executes the query, reading one record at a time and converting it to the specified type.
		/// </summary>
		public IAsyncEnumerable<T> EnumerateAsync<T>(CancellationToken cancellationToken = default) =>
			DoEnumerateAsync<T>(null, cancellationToken);

		/// <summary>
		/// Executes the query, reading one record at a time and converting it to the specified type with the specified delegate.
		/// </summary>
		public IAsyncEnumerable<T> EnumerateAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			DoEnumerateAsync(read ?? throw new ArgumentNullException(nameof(read)), cancellationToken);
#endif

		/// <summary>
		/// Executes the query, preparing to read multiple result sets.
		/// </summary>
		public DbConnectorResultSets QueryMultiple()
		{
			var command = Create();
			return new DbConnectorResultSets(command, command.ExecuteReader(), m_connector.ProviderMethods);
		}

		/// <summary>
		/// Executes the query, preparing to read multiple result sets.
		/// </summary>
		public async Task<DbConnectorResultSets> QueryMultipleAsync(CancellationToken cancellationToken = default)
		{
			var methods = m_connector.ProviderMethods;
			var command = Create();
			return new DbConnectorResultSets(command, await methods.ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false), methods);
		}

		/// <summary>
		/// Creates an <see cref="IDbCommand" /> from the text and parameters.
		/// </summary>
		public IDbCommand Create()
		{
			Validate();
			var connection = m_connector.Connection;
			return DoCreate(connection);
		}

		/// <summary>
		/// Creates an <see cref="IDbCommand" /> from the text and parameters.
		/// </summary>
		public async Task<IDbCommand> CreateAsync(CancellationToken cancellationToken = default)
		{
			Validate();
			var connection = await m_connector.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
			return DoCreate(connection);
		}

		internal DbConnectorCommand(DbConnector connector, string text, DbParameters parameters)
		{
			m_connector = connector;
			Text = text;
			Parameters = parameters;
		}

		private void Validate()
		{
			if (m_connector == null)
				throw new InvalidOperationException("Use DbConnector.Command() to create commands.");
		}

		private IDbCommand DoCreate(IDbConnection connection)
		{
			var command = connection.CreateCommand();
			command.CommandText = Text;

			var transaction = m_connector.Transaction;
			if (transaction != null)
				command.Transaction = transaction;

			foreach (var (name, value) in Parameters)
			{
				if (!(value is IDbDataParameter dbParameter))
				{
					dbParameter = command.CreateParameter();
					dbParameter.Value = value ?? DBNull.Value;
				}

				if (name != null)
					dbParameter.ParameterName = name;

				command.Parameters.Add(dbParameter);
			}

			return command;
		}

		private IReadOnlyList<T> DoQuery<T>(Func<IDataRecord, T>? read)
		{
			using var command = Create();
			using var reader = command.ExecuteReader();

			var list = new List<T>();

			do
			{
				while (reader.Read())
					list.Add(read != null ? read(reader) : reader.Get<T>());
			} while (reader.NextResult());

			return list;
		}

		[return: MaybeNull]
		private T DoQueryFirst<T>(Func<IDataRecord, T>? read, bool single, bool orDefault)
		{
			var commandBehavior = single ? CommandBehavior.SingleResult | CommandBehavior.SingleRow : CommandBehavior.SingleResult;

			using var command = Create();
			using var reader = command.ExecuteReader(commandBehavior);

			while (!reader.Read())
			{
				if (!reader.NextResult())
					return orDefault ? default(T)! : throw new InvalidOperationException("No records were found; use 'OrDefault' to permit this.");
			}

			var value = read != null ? read(reader) : reader.Get<T>();

			if (single && reader.Read())
				throw new InvalidOperationException("Additional records were found; use 'First' to permit this.");

			if (single && reader.NextResult())
				throw new InvalidOperationException("Additional results were found; use 'First' to permit this.");

			return value;
		}

		private async Task<IReadOnlyList<T>> DoQueryAsync<T>(Func<IDataRecord, T>? read, CancellationToken cancellationToken)
		{
			var methods = m_connector.ProviderMethods;

			using var command = await CreateAsync(cancellationToken).ConfigureAwait(false);
			using var reader = await methods.ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);

			var list = new List<T>();

			do
			{
				while (await methods.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
					list.Add(read != null ? read(reader) : reader.Get<T>());
			} while (await methods.NextResultAsync(reader, cancellationToken).ConfigureAwait(false));

			return list;
		}

		private async Task<T> DoQueryFirstAsync<T>(Func<IDataRecord, T>? read, bool single, bool orDefault, CancellationToken cancellationToken)
		{
			var methods = m_connector.ProviderMethods;
			var commandBehavior = single ? CommandBehavior.SingleResult | CommandBehavior.SingleRow : CommandBehavior.SingleResult;

			using var command = await CreateAsync(cancellationToken).ConfigureAwait(false);
			using var reader = await methods.ExecuteReaderAsync(command, commandBehavior, cancellationToken).ConfigureAwait(false);

			while (!await methods.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
			{
				if (!await methods.NextResultAsync(reader, cancellationToken).ConfigureAwait(false))
					return orDefault ? default(T)! : throw new InvalidOperationException("No records were found; use 'OrDefault' to permit this.");
			}

			var value = read != null ? read(reader) : reader.Get<T>();

			if (single && await methods.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
				throw new InvalidOperationException("Additional records were found; use 'First' to permit this.");

			if (single && await methods.NextResultAsync(reader, cancellationToken).ConfigureAwait(false))
				throw new InvalidOperationException("Additional results were found; use 'First' to permit this.");

			return value;
		}

		private IEnumerable<T> DoEnumerate<T>(Func<IDataRecord, T>? read)
		{
			using var command = Create();
			using var reader = command.ExecuteReader();

			do
			{
				while (reader.Read())
					yield return read != null ? read(reader) : reader.Get<T>();
			} while (reader.NextResult());
		}

#if NETSTANDARD2_1
		private async IAsyncEnumerable<T> DoEnumerateAsync<T>(Func<IDataRecord, T>? read, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var methods = m_connector.ProviderMethods;

			using var command = await CreateAsync(cancellationToken).ConfigureAwait(false);
			using var reader = await methods.ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);

			do
			{
				while (await methods.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
					yield return read != null ? read(reader) : reader.Get<T>();
			} while (await methods.NextResultAsync(reader, cancellationToken).ConfigureAwait(false));
		}
#endif

		private readonly DbConnector m_connector;
	}
}
