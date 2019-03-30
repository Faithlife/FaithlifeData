using System;
using System.Data;

namespace Faithlife.Data
{
	public static class DataRecordExtensions
	{
		public static T Get<T>(this IDataRecord record) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), 0, record.FieldCount);

		public static T Get<T>(this IDataRecord record, int index) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), index, 1);

		public static T Get<T>(this IDataRecord record, int index, int length) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), index, length);

		public static T Get<T>(this IDataRecord record, string name) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), record.GetOrdinal(name), 1);

		public static T Get<T>(this IDataRecord record, string name, int length) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), record.GetOrdinal(name), length);

		public static T Get<T>(this IDataRecord record, string fromName, string toName)
		{
			if (record == null)
				throw new ArgumentNullException(nameof(record));
			int fromIndex = record.GetOrdinal(fromName);
			int toIndex = record.GetOrdinal(toName);
			return DataRecordUtility.GetValue<T>(record, fromIndex, toIndex - fromIndex + 1);
		}
	}
}
