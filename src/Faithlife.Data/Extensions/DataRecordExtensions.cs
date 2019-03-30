using System;
using System.Data;

namespace Faithlife.Data.Extensions
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

		public static T Slice<T>(this IDataRecord record, int start)
		{
			int fieldCount = (record ?? throw new ArgumentNullException(nameof(record))).FieldCount;
			int actualStart = start >= 0 ? start : fieldCount + start;
			if (actualStart < 0 || actualStart > fieldCount)
				throw new ArgumentException($"Slice start {start} is out of range for {fieldCount} fields.");

			return DataRecordUtility.GetValue<T>(record, actualStart, fieldCount - actualStart);
		}

		public static T Slice<T>(this IDataRecord record, int start, int end)
		{
			int fieldCount = (record ?? throw new ArgumentNullException(nameof(record))).FieldCount;
			int actualStart = start >= 0 ? start : fieldCount + start;
			int actualEnd = end >= 0 ? end : fieldCount + end;
			if (actualStart < 0 || actualEnd < 0 || actualStart > actualEnd || actualEnd > fieldCount)
				throw new ArgumentException($"Slice start {start} and end {end} are out of range for {fieldCount} fields.");

			return DataRecordUtility.GetValue<T>(record, actualStart, actualEnd - actualStart);
		}
	}
}
