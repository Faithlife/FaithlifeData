using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	public class DbConnectorCommand
	{
		public int Execute()
		{
			using (var command = Create())
				return command.ExecuteNonQuery();
		}

		public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
		{
			using (var command = await CreateAsync(cancellationToken).ConfigureAwait(false))
				return await m_connector.ProviderMethods.ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
		}

		public IReadOnlyList<T> Query<T>() =>
			DoQuery<T>(null, null);

		public IReadOnlyList<T> Query<T>(Func<IDataRecord, T> read) =>
			DoQuery(read, null);

		public async Task<IReadOnlyList<T>> QueryAsync<T>(CancellationToken cancellationToken = default) =>
			await DoQueryAsync<T>(null, null, cancellationToken).ConfigureAwait(false);

		public async Task<IReadOnlyList<T>> QueryAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			await DoQueryAsync(read, null, cancellationToken).ConfigureAwait(false);

		public T QueryFirst<T>() =>
			DoQuery<T>(null, CommandBehavior.SingleResult | CommandBehavior.SingleRow).First();

		public T QueryFirst<T>(Func<IDataRecord, T> read) =>
			DoQuery(read, CommandBehavior.SingleResult | CommandBehavior.SingleRow).First();

		public async Task<T> QueryFirstAsync<T>(CancellationToken cancellationToken = default) =>
			(await DoQueryAsync<T>(null, CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false)).First();

		public async Task<T> QueryFirstAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			(await DoQueryAsync(read, CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false)).First();

		public T QueryFirstOrDefault<T>() =>
			DoQuery<T>(null, CommandBehavior.SingleResult | CommandBehavior.SingleRow).FirstOrDefault();

		public T QueryFirstOrDefault<T>(Func<IDataRecord, T> read) =>
			DoQuery(read, CommandBehavior.SingleResult | CommandBehavior.SingleRow).FirstOrDefault();

		public async Task<T> QueryFirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
			(await DoQueryAsync<T>(null, CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false)).FirstOrDefault();

		public async Task<T> QueryFirstOrDefaultAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			(await DoQueryAsync(read, CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false)).FirstOrDefault();

		public IDbCommand Create()
		{
			var connection = m_connector.Connection;
			var command = connection.CreateCommand();
			command.CommandText = m_text;

			var transaction = m_connector.Transaction;
			if (transaction != null)
				command.Transaction = transaction;

			if (m_parameters != null)
			{
				foreach (var (name, value) in m_parameters)
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
			}

			return command;
		}

		public async Task<IDbCommand> CreateAsync(CancellationToken cancellationToken = default)
		{
			var connection = await m_connector.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
			var command = connection.CreateCommand();
			command.CommandText = m_text;

			var transaction = m_connector.Transaction;
			if (transaction != null)
				command.Transaction = transaction;

			return command;
		}

		internal DbConnectorCommand(DbConnector connector, string text, DbParameters parameters)
		{
			m_connector = connector;
			m_text = text;
			m_parameters = parameters;
		}

		private IReadOnlyList<T> DoQuery<T>(Func<IDataRecord, T> read, CommandBehavior? commandBehavior)
		{
			var list = new List<T>();
			using (var command = Create())
			using (var reader = commandBehavior == null ? command.ExecuteReader() : command.ExecuteReader(commandBehavior.Value))
			{
				do
				{
					while (reader.Read())
						list.Add(read != null ? read(reader) : reader.Get<T>());
				} while (reader.NextResult());
			}
			return list;
		}

		private async Task<IReadOnlyList<T>> DoQueryAsync<T>(Func<IDataRecord, T> read, CommandBehavior? commandBehavior, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			using (var command = await CreateAsync(cancellationToken).ConfigureAwait(false))
			using (var reader = commandBehavior == null ? await m_connector.ProviderMethods.ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false) : await m_connector.ProviderMethods.ExecuteReaderAsync(command, commandBehavior.Value, cancellationToken).ConfigureAwait(false))
			{
				do
				{
					while (reader.Read())
						list.Add(read != null ? read(reader) : reader.Get<T>());
				} while (reader.NextResult());
			}
			return list;
		}

		private readonly DbConnector m_connector;
		private readonly string m_text;
		private readonly DbParameters m_parameters;
	}
}
