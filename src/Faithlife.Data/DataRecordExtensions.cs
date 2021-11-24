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
			Get<T>(record ?? throw new ArgumentNullException(nameof(record)), 0, record.FieldCount);

		/// <summary>
		/// Converts the specified record field to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, int index) =>
			Get<T>(record ?? throw new ArgumentNullException(nameof(record)), index, 1);

		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, int index, int count)
		{
			var fieldCount = (record ?? throw new ArgumentNullException(nameof(record))).FieldCount;
			if (index < 0 || count < 0 || index > fieldCount - count)
				throw new ArgumentException($"Index {index} and count {count} are out of range for {fieldCount} fields.");
			return DbValueTypeInfo.GetInfo<T>().GetValue(record, index, count);
		}

		/// <summary>
		/// Converts the specified record field to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, string name) =>
			Get<T>(record ?? throw new ArgumentNullException(nameof(record)), record.GetOrdinal(name), 1);

		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, string name, int count) =>
			Get<T>(record ?? throw new ArgumentNullException(nameof(record)), record.GetOrdinal(name), count);

		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, string fromName, string toName)
		{
			if (record == null)
				throw new ArgumentNullException(nameof(record));

			var fromIndex = record.GetOrdinal(fromName);
			var toIndex = record.GetOrdinal(toName);
			return Get<T>(record, fromIndex, toIndex - fromIndex + 1);
		}

		/// <summary>
		/// Converts the specified record field to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, Index index) =>
			Get<T>(record ?? throw new ArgumentNullException(nameof(record)), index.GetOffset(record.FieldCount), 1);

		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
		public static T Get<T>(this IDataRecord record, Range range)
		{
			if (record == null)
				throw new ArgumentNullException(nameof(record));

			var (index, count) = range.GetOffsetAndLength(record.FieldCount);
			return Get<T>(record, index, count);
		}
	}
}
