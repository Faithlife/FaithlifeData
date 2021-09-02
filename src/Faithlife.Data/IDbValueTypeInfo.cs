using System;
using System.Data;

namespace Faithlife.Data
{
	internal interface IDbValueTypeInfo
	{
		Type Type { get; }

		int? FieldCount { get; }

		object? GetValue(IDataRecord record, int index, int count);

		string? GetColumnAttributeName(string propertyName);
	}
}
