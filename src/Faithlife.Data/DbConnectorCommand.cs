using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
		/// The connector of the command.
		/// </summary>
		public DbConnector Connector { get; }

		/// <summary>
		/// True after <see cref="Cache"/> is called.
		/// </summary>
		public bool IsCached { get; }

		/// <summary>
		/// Executes the command, returning the number of rows affected.
		/// </summary>
		/// <seealso cref="ExecuteAsync" />
		public int Execute()
		{
			using var command = Create();
			return command.ExecuteNonQuery();
		}

		/// <summary>
		/// Executes the command, returning the number of rows affected.
		/// </summary>
		/// <seealso cref="Execute" />
		public async ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default)
		{
			using var command = await CreateAsync(cancellationToken).ConfigureAwait(false);
			return await Connector.ProviderMethods.ExecuteNonQueryAsync(CachedCommand.Unwrap(command), cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Executes the query, reading every record and converting it to the specified type.
		/// </summary>
		/// <seealso cref="QueryAsync{T}(CancellationToken)" />
		public IReadOnlyList<T> Query<T>() =>
			DoQuery<T>(null);

		/// <summary>
		/// Executes the query, reading every record and converting it to the specified type with the specified delegate.
		/// </summary>
		/// <seealso cref="QueryAsync{T}(Func{IDataRecord, T}, CancellationToken)" />
		public IReadOnlyList<T> Query<T>(Func<IDataRecord, T> map) =>
			DoQuery(map ?? throw new ArgumentNullException(nameof(map)));

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
		/// <seealso cref="QueryFirstAsync{T}(CancellationToken)" />
		public T QueryFirst<T>() =>
			DoQueryFirst<T>(null, single: false, orDefault: false);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
		/// <seealso cref="QueryFirstAsync{T}(Func{IDataRecord, T}, CancellationToken)" />
		public T QueryFirst<T>(Func<IDataRecord, T> map) =>
			DoQueryFirst(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: false);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
		/// <seealso cref="QueryFirstOrDefaultAsync{T}(CancellationToken)" />
		public T QueryFirstOrDefault<T>() =>
			DoQueryFirst<T>(null, single: false, orDefault: true);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
		/// <seealso cref="QueryFirstOrDefaultAsync{T}(Func{IDataRecord, T}, CancellationToken)" />
		public T QueryFirstOrDefault<T>(Func<IDataRecord, T> map) =>
			DoQueryFirst(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: true);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
		/// <seealso cref="QuerySingleAsync{T}(CancellationToken)" />
		public T QuerySingle<T>() =>
			DoQueryFirst<T>(null, single: true, orDefault: false);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
		/// <seealso cref="QuerySingleAsync{T}(Func{IDataRecord, T}, CancellationToken)" />
		public T QuerySingle<T>(Func<IDataRecord, T> map) =>
			DoQueryFirst(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: false);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.
		/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
		/// <seealso cref="QuerySingleOrDefaultAsync{T}(CancellationToken)" />
		public T QuerySingleOrDefault<T>() =>
			DoQueryFirst<T>(null, single: true, orDefault: true);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.
		/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
		/// <seealso cref="QuerySingleOrDefaultAsync{T}(Func{IDataRecord, T}, CancellationToken)" />
		public T QuerySingleOrDefault<T>(Func<IDataRecord, T> map) =>
			DoQueryFirst(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: true);

		/// <summary>
		/// Executes the query, converting each record to the specified type.
		/// </summary>
		/// <seealso cref="Query{T}()" />
		public ValueTask<IReadOnlyList<T>> QueryAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryAsync<T>(null, cancellationToken);

		/// <summary>
		/// Executes the query, converting each record to the specified type with the specified delegate.
		/// </summary>
		/// <seealso cref="Query{T}(Func{IDataRecord, T})" />
		public ValueTask<IReadOnlyList<T>> QueryAsync<T>(Func<IDataRecord, T> map, CancellationToken cancellationToken = default) =>
			DoQueryAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
		/// <seealso cref="QueryFirst{T}()" />
		public ValueTask<T> QueryFirstAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync<T>(null, single: false, orDefault: false, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
		/// <seealso cref="QueryFirst{T}(Func{IDataRecord, T})" />
		public ValueTask<T> QueryFirstAsync<T>(Func<IDataRecord, T> map, CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: false, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
		/// <seealso cref="QueryFirstOrDefault{T}()" />
		public ValueTask<T> QueryFirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync<T>(null, single: false, orDefault: true, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
		/// <seealso cref="QueryFirstOrDefault{T}(Func{IDataRecord, T})" />
		public ValueTask<T> QueryFirstOrDefaultAsync<T>(Func<IDataRecord, T> map, CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: true, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
		/// <seealso cref="QuerySingle{T}()" />
		public ValueTask<T> QuerySingleAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync<T>(null, single: true, orDefault: false, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
		/// <seealso cref="QuerySingle{T}(Func{IDataRecord, T})" />
		public ValueTask<T> QuerySingleAsync<T>(Func<IDataRecord, T> map, CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: false, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.
		/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
		/// <seealso cref="QuerySingleOrDefault{T}()" />
		public ValueTask<T> QuerySingleOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync<T>(null, single: true, orDefault: true, cancellationToken);

		/// <summary>
		/// Executes the query, converting the first record to the specified type with the specified delegate.
		/// </summary>
		/// <remarks>Returns <c>default(T)</c> if no records are returned.
		/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
		/// <seealso cref="QuerySingleOrDefault{T}(Func{IDataRecord, T})" />
		public ValueTask<T> QuerySingleOrDefaultAsync<T>(Func<IDataRecord, T> map, CancellationToken cancellationToken = default) =>
			DoQueryFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: true, cancellationToken);

		/// <summary>
		/// Executes the query, reading one record at a time and converting it to the specified type.
		/// </summary>
		/// <seealso cref="EnumerateAsync{T}(CancellationToken)" />
		public IEnumerable<T> Enumerate<T>() =>
			DoEnumerate<T>(null);

		/// <summary>
		/// Executes the query, reading one record at a time and converting it to the specified type with the specified delegate.
		/// </summary>
		/// <seealso cref="EnumerateAsync{T}(Func{IDataRecord, T}, CancellationToken)" />
		public IEnumerable<T> Enumerate<T>(Func<IDataRecord, T> map) =>
			DoEnumerate(map ?? throw new ArgumentNullException(nameof(map)));

		/// <summary>
		/// Executes the query, reading one record at a time and converting it to the specified type.
		/// </summary>
		/// <seealso cref="Enumerate{T}()" />
		public IAsyncEnumerable<T> EnumerateAsync<T>(CancellationToken cancellationToken = default) =>
			DoEnumerateAsync<T>(null, cancellationToken);

		/// <summary>
		/// Executes the query, reading one record at a time and converting it to the specified type with the specified delegate.
		/// </summary>
		/// <seealso cref="Enumerate{T}(Func{IDataRecord, T})" />
		public IAsyncEnumerable<T> EnumerateAsync<T>(Func<IDataRecord, T> map, CancellationToken cancellationToken = default) =>
			DoEnumerateAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

		/// <summary>
		/// Executes the query, preparing to read multiple result sets.
		/// </summary>
		/// <seealso cref="QueryMultipleAsync" />
		public DbConnectorResultSets QueryMultiple()
		{
			var command = Create();
			return new DbConnectorResultSets(command, command.ExecuteReader(), Connector.ProviderMethods);
		}

		/// <summary>
		/// Executes the query, preparing to read multiple result sets.
		/// </summary>
		/// <seealso cref="QueryMultiple" />
		public async ValueTask<DbConnectorResultSets> QueryMultipleAsync(CancellationToken cancellationToken = default)
		{
			var methods = Connector.ProviderMethods;
			var command = await CreateAsync(cancellationToken).ConfigureAwait(false);
			return new DbConnectorResultSets(command, await methods.ExecuteReaderAsync(CachedCommand.Unwrap(command), cancellationToken).ConfigureAwait(false), methods);
		}

		/// <summary>
		/// Caches the command.
		/// </summary>
		public DbConnectorCommand Cache() => new DbConnectorCommand(Connector, Text, Parameters, isCached: true);

		/// <summary>
		/// Creates an <see cref="IDbCommand" /> from the text and parameters.
		/// </summary>
		/// <seealso cref="CreateAsync" />
		public IDbCommand Create()
		{
			Validate();
			var connection = Connector.Connection;
			return DoCreate(connection);
		}

		/// <summary>
		/// Creates an <see cref="IDbCommand" /> from the text and parameters.
		/// </summary>
		/// <seealso cref="Create" />
		public async ValueTask<IDbCommand> CreateAsync(CancellationToken cancellationToken = default)
		{
			Validate();
			var connection = await Connector.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
			return DoCreate(connection);
		}

		internal DbConnectorCommand(DbConnector connector, string text, DbParameters parameters, bool isCached = false)
		{
			Connector = connector;
			Text = text;
			Parameters = parameters;
			IsCached = isCached;
		}

		private void Validate()
		{
			if (Connector == null)
				throw new InvalidOperationException("Use DbConnector to create commands.");
		}

		private IDbCommand DoCreate(IDbConnection connection)
		{
			var commandText = Text;

			var parameters = Parameters;
			var index = 0;
			while (index < parameters.Count)
			{
				// look for @name... in SQL for collection parameters
				var (name, value) = parameters[index];
				if (!string.IsNullOrEmpty(name) && !(value is string) && !(value is byte[]) && value is IEnumerable list)
				{
					var itemCount = -1;
					var replacements = new List<(string Name, object? Value)>();

					string Replacement(Match match)
					{
						if (itemCount == -1)
						{
							itemCount = 0;

							foreach (var item in list)
							{
								replacements.Add(($"{name}_{itemCount}", item));
								itemCount++;
							}

							if (itemCount == 0)
								throw new InvalidOperationException("Collection parameter '{name}' must not be empty.");
						}

						return string.Join(",", Enumerable.Range(0, itemCount).Select(x => $"{match.Groups[1]}_{x}"));
					}

					commandText = Regex.Replace(commandText, $@"([?@:]{Regex.Escape(name)})\.\.\.",
						Replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

					// if special syntax wasn't found, leave the parameter alone, for databases that support collections directly
					if (itemCount != -1)
					{
						parameters = DbParameters.Create(parameters.Take(index).Concat(replacements).Concat(parameters.Skip(index + 1)));
						index += replacements.Count;
					}
					else
					{
						index += 1;
					}
				}
				else
				{
					index += 1;
				}
			}

			IDbCommand command;
			var transaction = Connector.Transaction;

			var cache = IsCached ? Connector.CommandCache : null;
			if (cache != null)
			{
				if (cache.TryGetValue(commandText, out command))
				{
					command.Parameters.Clear();
					command.Transaction = transaction;
				}
				else
				{
					command = new CachedCommand(CreateNewCommand());
					cache.Add(commandText, command);
				}
			}
			else
			{
				command = CreateNewCommand();
			}

			foreach (var (name, value) in parameters)
			{
				if (!(value is IDbDataParameter dbParameter))
				{
					dbParameter = command.CreateParameter();
					dbParameter.Value = value ?? DBNull.Value;
				}

				dbParameter.ParameterName = name;

				command.Parameters.Add(dbParameter);
			}

			return command;

			IDbCommand CreateNewCommand()
			{
				var newCommand = connection.CreateCommand();
				newCommand.CommandText = commandText;
				if (transaction != null)
					newCommand.Transaction = transaction;
				return newCommand;
			}
		}

		private IReadOnlyList<T> DoQuery<T>(Func<IDataRecord, T>? map)
		{
			using var command = Create();
			using var reader = command.ExecuteReader();

			var list = new List<T>();

			do
			{
				while (reader.Read())
					list.Add(map != null ? map(reader) : reader.Get<T>());
			}
			while (reader.NextResult());

			return list;
		}

		private async ValueTask<IReadOnlyList<T>> DoQueryAsync<T>(Func<IDataRecord, T>? map, CancellationToken cancellationToken)
		{
			var methods = Connector.ProviderMethods;

			using var command = await CreateAsync(cancellationToken).ConfigureAwait(false);
			using var reader = await methods.ExecuteReaderAsync(CachedCommand.Unwrap(command), cancellationToken).ConfigureAwait(false);

			var list = new List<T>();

			do
			{
				while (await methods.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
					list.Add(map != null ? map(reader) : reader.Get<T>());
			}
			while (await methods.NextResultAsync(reader, cancellationToken).ConfigureAwait(false));

			return list;
		}

		private T DoQueryFirst<T>(Func<IDataRecord, T>? map, bool single, bool orDefault)
		{
			using var command = Create();
			using var reader = single ? command.ExecuteReader() : command.ExecuteReader(CommandBehavior.SingleRow);

			while (!reader.Read())
			{
				if (!reader.NextResult())
					return orDefault ? default(T)! : throw new InvalidOperationException("No records were found; use 'OrDefault' to permit this.");
			}

			var value = map != null ? map(reader) : reader.Get<T>();

			if (single && reader.Read())
				throw CreateTooManyRecordsException();

			if (single && reader.NextResult())
				throw CreateTooManyRecordsException();

			return value;
		}

		private async ValueTask<T> DoQueryFirstAsync<T>(Func<IDataRecord, T>? map, bool single, bool orDefault, CancellationToken cancellationToken)
		{
			var methods = Connector.ProviderMethods;

			using var command = await CreateAsync(cancellationToken).ConfigureAwait(false);
			using var reader = single ? await methods.ExecuteReaderAsync(CachedCommand.Unwrap(command), cancellationToken).ConfigureAwait(false) : await methods.ExecuteReaderAsync(CachedCommand.Unwrap(command), CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);

			while (!await methods.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
			{
				if (!await methods.NextResultAsync(reader, cancellationToken).ConfigureAwait(false))
					return orDefault ? default(T)! : throw CreateNoRecordsException();
			}

			var value = map != null ? map(reader) : reader.Get<T>();

			if (single && await methods.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
				throw CreateTooManyRecordsException();

			if (single && await methods.NextResultAsync(reader, cancellationToken).ConfigureAwait(false))
				throw CreateTooManyRecordsException();

			return value;
		}

		private static InvalidOperationException CreateNoRecordsException() =>
			new InvalidOperationException("No records were found; use 'OrDefault' to permit this.");

		private static InvalidOperationException CreateTooManyRecordsException() =>
			new InvalidOperationException("Additional records were found; use 'First' to permit this.");

		private IEnumerable<T> DoEnumerate<T>(Func<IDataRecord, T>? map)
		{
			using var command = Create();
			using var reader = command.ExecuteReader();

			do
			{
				while (reader.Read())
					yield return map != null ? map(reader) : reader.Get<T>();
			}
			while (reader.NextResult());
		}

		private async IAsyncEnumerable<T> DoEnumerateAsync<T>(Func<IDataRecord, T>? map, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var methods = Connector.ProviderMethods;

			using var command = await CreateAsync(cancellationToken).ConfigureAwait(false);
			using var reader = await methods.ExecuteReaderAsync(CachedCommand.Unwrap(command), cancellationToken).ConfigureAwait(false);

			do
			{
				while (await methods.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
					yield return map != null ? map(reader) : reader.Get<T>();
			}
			while (await methods.NextResultAsync(reader, cancellationToken).ConfigureAwait(false));
		}
	}
}
