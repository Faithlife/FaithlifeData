using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	/// <summary>
	/// Encapsulates multiple result sets.
	/// </summary>
	public sealed class DbConnectorResultSets : IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Reads a result set, converting each record to the specified type.
		/// </summary>
		/// <seealso cref="ReadAsync{T}(CancellationToken)" />
		public IReadOnlyList<T> Read<T>() =>
			DoRead<T>(null);

		/// <summary>
		/// Reads a result set, converting each record to the specified type with the specified delegate.
		/// </summary>
		/// <seealso cref="ReadAsync{T}(Func{IDataRecord, T}, CancellationToken)" />
		public IReadOnlyList<T> Read<T>(Func<IDataRecord, T> map) =>
			DoRead(map ?? throw new ArgumentNullException(nameof(map)));

		/// <summary>
		/// Reads a result set, converting each record to the specified type.
		/// </summary>
		/// <seealso cref="Read{T}()" />
		public ValueTask<IReadOnlyList<T>> ReadAsync<T>(CancellationToken cancellationToken = default) =>
			DoReadAsync<T>(null, cancellationToken);

		/// <summary>
		/// Reads a result set, converting each record to the specified type with the specified delegate.
		/// </summary>
		/// <seealso cref="Read{T}(Func{IDataRecord, T})" />
		public ValueTask<IReadOnlyList<T>> ReadAsync<T>(Func<IDataRecord, T> map, CancellationToken cancellationToken = default) =>
			DoReadAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

		/// <summary>
		/// Reads a result set, reading one record at a time and converting it to the specified type.
		/// </summary>
		/// <seealso cref="EnumerateAsync{T}(CancellationToken)" />
		public IEnumerable<T> Enumerate<T>() =>
			DoEnumerate<T>(null);

		/// <summary>
		/// Reads a result set, reading one record at a time and converting it to the specified type with the specified delegate.
		/// </summary>
		/// <seealso cref="EnumerateAsync{T}(Func{IDataRecord, T}, CancellationToken)" />
		public IEnumerable<T> Enumerate<T>(Func<IDataRecord, T> map) =>
			DoEnumerate(map ?? throw new ArgumentNullException(nameof(map)));

		/// <summary>
		/// Reads a result set, reading one record at a time and converting it to the specified type.
		/// </summary>
		/// <seealso cref="Enumerate{T}()" />
		public IAsyncEnumerable<T> EnumerateAsync<T>(CancellationToken cancellationToken = default) =>
			DoEnumerateAsync<T>(null, cancellationToken);

		/// <summary>
		/// Reads a result set, reading one record at a time and converting it to the specified type with the specified delegate.
		/// </summary>
		/// <seealso cref="Enumerate{T}(Func{IDataRecord, T})" />
		public IAsyncEnumerable<T> EnumerateAsync<T>(Func<IDataRecord, T> map, CancellationToken cancellationToken = default) =>
			DoEnumerateAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

		/// <summary>
		/// Disposes resources used by the result sets.
		/// </summary>
		/// <seealso cref="DisposeAsync" />
		public void Dispose()
		{
			m_reader.Dispose();
			m_command.Dispose();
		}

		/// <summary>
		/// Disposes resources used by the result sets.
		/// </summary>
		/// <seealso cref="Dispose" />
		public async ValueTask DisposeAsync()
		{
			await m_methods.DisposeReaderAsync(m_reader).ConfigureAwait(false);
			await m_methods.DisposeCommandAsync(CachedCommand.Unwrap(m_command)).ConfigureAwait(false);
		}

		internal DbConnectorResultSets(IDbCommand command, IDataReader reader, DbProviderMethods methods)
		{
			m_command = command;
			m_reader = reader;
			m_methods = methods;
		}

		private IReadOnlyList<T> DoRead<T>(Func<IDataRecord, T>? map)
		{
			if (m_next && !m_reader.NextResult())
				throw CreateNoMoreResultsException();
			m_next = true;

			var list = new List<T>();
			while (m_reader.Read())
				list.Add(map != null ? map(m_reader) : m_reader.Get<T>());
			return list;
		}

		private async ValueTask<IReadOnlyList<T>> DoReadAsync<T>(Func<IDataRecord, T>? map, CancellationToken cancellationToken)
		{
			if (m_next && !await m_methods.NextResultAsync(m_reader, cancellationToken).ConfigureAwait(false))
				throw CreateNoMoreResultsException();
			m_next = true;

			var list = new List<T>();
			while (await m_methods.ReadAsync(m_reader, cancellationToken).ConfigureAwait(false))
				list.Add(map != null ? map(m_reader) : m_reader.Get<T>());
			return list;
		}

		private IEnumerable<T> DoEnumerate<T>(Func<IDataRecord, T>? map)
		{
			if (m_next && !m_reader.NextResult())
				throw CreateNoMoreResultsException();
			m_next = true;

			while (m_reader.Read())
				yield return map != null ? map(m_reader) : m_reader.Get<T>();
		}

		private async IAsyncEnumerable<T> DoEnumerateAsync<T>(Func<IDataRecord, T>? map, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			if (m_next && !await m_methods.NextResultAsync(m_reader, cancellationToken).ConfigureAwait(false))
				throw CreateNoMoreResultsException();
			m_next = true;

			while (await m_methods.ReadAsync(m_reader, cancellationToken).ConfigureAwait(false))
				yield return map != null ? map(m_reader) : m_reader.Get<T>();
		}

		private static InvalidOperationException CreateNoMoreResultsException() =>
			new InvalidOperationException("No more results.");

		private readonly IDbCommand m_command;
		private readonly IDataReader m_reader;
		private readonly DbProviderMethods m_methods;
		private bool m_next;
	}
}
