using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	/// <summary>
	/// Encapsulates multiple result sets.
	/// </summary>
	public sealed class DbConnectorResultSets : IDisposable
	{
		/// <summary>
		/// Reads a result set, converting each record to the specified type.
		/// </summary>
		public IReadOnlyList<T> Read<T>() =>
			DoRead<T>(null);

		/// <summary>
		/// Reads a result set, converting each record to the specified type with the specified delegate.
		/// </summary>
		public IReadOnlyList<T> Read<T>(Func<IDataRecord, T> read) =>
			DoRead(read ?? throw new ArgumentNullException(nameof(read)));

		/// <summary>
		/// Reads a result set, converting each record to the specified type.
		/// </summary>
		public async Task<IReadOnlyList<T>> ReadAsync<T>(CancellationToken cancellationToken = default) =>
			await DoReadAsync<T>(null, cancellationToken);

		/// <summary>
		/// Reads a result set, converting each record to the specified type with the specified delegate.
		/// </summary>
		public async Task<IReadOnlyList<T>> ReadAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			await DoReadAsync(read ?? throw new ArgumentNullException(nameof(read)), cancellationToken);

		/// <summary>
		/// Disposes resources used by the result sets.
		/// </summary>
		public void Dispose()
		{
			m_reader.Dispose();
			m_command.Dispose();
		}

		internal DbConnectorResultSets(IDbCommand command, IDataReader reader, DbProviderMethods methods)
		{
			m_command = command;
			m_reader = reader;
			m_methods = methods;
		}

		private IReadOnlyList<T> DoRead<T>(Func<IDataRecord, T> read)
		{
			if (m_done)
				throw new InvalidOperationException("No more results.");

			var list = new List<T>();
			while (m_reader.Read())
				list.Add(read != null ? read(m_reader) : m_reader.Get<T>());
			m_done = !m_reader.NextResult();
			return list;
		}

		private async Task<IReadOnlyList<T>> DoReadAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken)
		{
			if (m_done)
				throw new InvalidOperationException("No more results.");

			var list = new List<T>();
			while (await m_methods.ReadAsync(m_reader, cancellationToken))
				list.Add(read != null ? read(m_reader) : m_reader.Get<T>());
			m_done = !await m_methods.NextResultAsync(m_reader, cancellationToken);
			return list;
		}

		private readonly IDbCommand m_command;
		private readonly IDataReader m_reader;
		private readonly DbProviderMethods m_methods;
		private bool m_done;
	}
}
