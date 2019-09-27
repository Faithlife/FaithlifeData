using System;
using System.Data;

namespace Faithlife.Data
{
	/// <summary>
	/// Utility methods for <see cref="IDataRecord" />.
	/// </summary>
	public static class DataRecordUtility
	{
		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
		public static T GetValue<T>(IDataRecord record, int start, int length)
		{
			CheckGetValueArgs(record, start, length);
			return DbValueTypeInfo.GetInfo<T>().GetValue(record, start, length);
		}

		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
		public static object GetValue(Type type, IDataRecord record, int start, int length)
		{
			CheckGetValueArgs(record, start, length);
			return DbValueTypeInfo.GetInfo(type).GetValue(record, start, length);
		}

		private static void CheckGetValueArgs(IDataRecord record, int start, int length)
		{
			int fieldCount = (record ?? throw new ArgumentNullException(nameof(record))).FieldCount;
			if (start < 0 || length < 0 || start > fieldCount - length)
				throw new ArgumentException($"Range start {start} and length {length} are out of range for {fieldCount} fields.");
		}
	}
}
