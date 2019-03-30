using System;
using System.Collections.Generic;
using System.Data;

namespace Faithlife.Data
{
	public sealed class DbConnectorResultSet : IDisposable
	{
		public IReadOnlyList<T> Read<T>() =>
			DoRead<T>(null, c_queryBehavior);

		public IReadOnlyList<T> Read<T>(Func<IDataRecord, T> read) =>
			DoRead(read, c_queryBehavior);

		public void Dispose()
		{
			m_reader.Dispose();
			m_command.Dispose();
		}

		internal DbConnectorResultSet(IDbCommand command, IDataReader reader)
		{
			m_command = command;
			m_reader = reader;
		}

		private IReadOnlyList<T> DoRead<T>(Func<IDataRecord, T> read, CommandBehavior commandBehavior)
		{
			if (m_done)
				throw new InvalidOperationException("No more results.");

			var list = new List<T>();
			while (m_reader.Read())
				list.Add(read != null ? read(m_reader) : m_reader.Get<T>());
			m_done = !m_reader.NextResult();
			return list;
		}

		private const CommandBehavior c_queryBehavior = CommandBehavior.SequentialAccess;

		private readonly IDbCommand m_command;
		private readonly IDataReader m_reader;
		private bool m_done;
	}
}
