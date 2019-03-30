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
			DoQuery<T>(null, c_queryBehavior);

		public IReadOnlyList<T> Query<T>(Func<IDataRecord, T> read) =>
			DoQuery(read, c_queryBehavior);

		public async Task<IReadOnlyList<T>> QueryAsync<T>(CancellationToken cancellationToken = default) =>
			await DoQueryAsync<T>(null, c_queryBehavior, cancellationToken).ConfigureAwait(false);

		public async Task<IReadOnlyList<T>> QueryAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			await DoQueryAsync(read, c_queryBehavior, cancellationToken).ConfigureAwait(false);

		public T QueryFirst<T>() =>
			DoQuery<T>(null, c_firstBehavior).First();

		public T QueryFirst<T>(Func<IDataRecord, T> read) =>
			DoQuery(read, c_firstBehavior).First();

		public async Task<T> QueryFirstAsync<T>(CancellationToken cancellationToken = default) =>
			(await DoQueryAsync<T>(null, c_firstBehavior, cancellationToken).ConfigureAwait(false)).First();

		public async Task<T> QueryFirstAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			(await DoQueryAsync(read, c_firstBehavior, cancellationToken).ConfigureAwait(false)).First();

		public T QueryFirstOrDefault<T>() =>
			DoQuery<T>(null, c_firstBehavior).FirstOrDefault();

		public T QueryFirstOrDefault<T>(Func<IDataRecord, T> read) =>
			DoQuery(read, c_firstBehavior).FirstOrDefault();

		public async Task<T> QueryFirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
			(await DoQueryAsync<T>(null, c_firstBehavior, cancellationToken).ConfigureAwait(false)).FirstOrDefault();

		public async Task<T> QueryFirstOrDefaultAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			(await DoQueryAsync(read, c_firstBehavior, cancellationToken).ConfigureAwait(false)).FirstOrDefault();

		public T QuerySingle<T>() =>
			DoQuery<T>(null, c_singleBehavior).Single();

		public T QuerySingle<T>(Func<IDataRecord, T> read) =>
			DoQuery(read, c_singleBehavior).Single();

		public async Task<T> QuerySingleAsync<T>(CancellationToken cancellationToken = default) =>
			(await DoQueryAsync<T>(null, c_singleBehavior, cancellationToken).ConfigureAwait(false)).Single();

		public async Task<T> QuerySingleAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			(await DoQueryAsync(read, c_singleBehavior, cancellationToken).ConfigureAwait(false)).Single();

		public T QuerySingleOrDefault<T>() =>
			DoQuery<T>(null, c_singleBehavior).SingleOrDefault();

		public T QuerySingleOrDefault<T>(Func<IDataRecord, T> read) =>
			DoQuery(read, c_singleBehavior).SingleOrDefault();

		public async Task<T> QuerySingleOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
			(await DoQueryAsync<T>(null, c_singleBehavior, cancellationToken).ConfigureAwait(false)).SingleOrDefault();

		public async Task<T> QuerySingleOrDefaultAsync<T>(Func<IDataRecord, T> read, CancellationToken cancellationToken = default) =>
			(await DoQueryAsync(read, c_singleBehavior, cancellationToken).ConfigureAwait(false)).SingleOrDefault();

		public IEnumerable<T> Enumerate<T>() =>
			DoEnumerate<T>(null, null);

		public IEnumerable<T> Enumerate<T>(Func<IDataRecord, T> read) =>
			DoEnumerate(read, null);

		public DbConnectorResultSet QueryMultiple()
		{
			var command = Create();
			var reader = command.ExecuteReader(c_queryBehavior);
			return new DbConnectorResultSet(command, reader);
		}

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

		private IReadOnlyList<T> DoQuery<T>(Func<IDataRecord, T> read, CommandBehavior commandBehavior)
		{
			var list = new List<T>();
			using (var command = Create())
			using (var reader = command.ExecuteReader(commandBehavior))
			{
				do
				{
					while (reader.Read())
						list.Add(read != null ? read(reader) : reader.Get<T>());
				} while (reader.NextResult());
			}
			return list;
		}

		private async Task<IReadOnlyList<T>> DoQueryAsync<T>(Func<IDataRecord, T> read, CommandBehavior commandBehavior, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			using (var command = await CreateAsync(cancellationToken).ConfigureAwait(false))
			using (var reader = await m_connector.ProviderMethods.ExecuteReaderAsync(command, commandBehavior, cancellationToken).ConfigureAwait(false))
			{
				do
				{
					while (reader.Read())
						list.Add(read != null ? read(reader) : reader.Get<T>());
				} while (reader.NextResult());
			}
			return list;
		}

		private IEnumerable<T> DoEnumerate<T>(Func<IDataRecord, T> read, CommandBehavior? commandBehavior)
		{
			using (var command = Create())
			using (var reader = commandBehavior == null ? command.ExecuteReader() : command.ExecuteReader(commandBehavior.Value))
			{
				do
				{
					while (reader.Read())
						yield return read != null ? read(reader) : reader.Get<T>();
				} while (reader.NextResult());
			}
		}

		private const CommandBehavior c_queryBehavior = CommandBehavior.SequentialAccess;
		private const CommandBehavior c_singleBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;
		private const CommandBehavior c_firstBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;

		private readonly DbConnector m_connector;
		private readonly string m_text;
		private readonly DbParameters m_parameters;
	}
}
