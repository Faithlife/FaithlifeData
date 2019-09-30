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
		public static T GetValue<T>(IDataRecord record, int index, int count)
		{
			CheckGetValueArgs(record, index, count);
			return DbValueTypeInfo.GetInfo<T>().GetValue(record, index, count);
		}

		/// <summary>
		/// Converts the specified record fields to the specified type.
		/// </summary>
		public static object GetValue(Type type, IDataRecord record, int index, int count)
		{
			CheckGetValueArgs(record, index, count);
			return DbValueTypeInfo.GetInfo(type).GetValue(record, index, count);
		}

		private static void CheckGetValueArgs(IDataRecord record, int index, int count)
		{
			int fieldCount = (record ?? throw new ArgumentNullException(nameof(record))).FieldCount;
			if (index < 0 || count < 0 || index > fieldCount - count)
				throw new ArgumentException($"Index {index} and count {count} are out of range for {fieldCount} fields.");
		}
	}
}
