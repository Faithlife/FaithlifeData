using System;
using System.Data;

namespace Faithlife.Data
{
	/// <summary>
	/// Extension methods for <see cref="IDataRecord" />.
	/// </summary>
	public static class DataRecordExtensions
	{
		/// <summary>
		/// Converts all record fields to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), 0, record.FieldCount);

		/// <summary>
		/// Converts the specified record field to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, int index) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), index, 1);

		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, int index, int count) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), index, count);

		/// <summary>
		/// Converts the specified record field to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, string name) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), record.GetOrdinal(name), 1);

		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, string name, int count) =>
			DataRecordUtility.GetValue<T>(record ?? throw new ArgumentNullException(nameof(record)), record.GetOrdinal(name), count);

		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
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
