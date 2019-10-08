using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Faithlife.Reflection;

namespace Faithlife.Data
{
	internal static class DbValueTypeInfo
	{
		public static DbValueTypeInfo<T> GetInfo<T>() => DbValueTypeInfo<T>.Instance;

		public static IDbValueTypeInfo GetInfo(Type type) => s_infos.GetOrAdd(type, CreateInfo);

		internal static DbValueTypeStrategy GetStrategy(Type type)
		{
			if (s_strategies.TryGetValue(type, out var strategy))
				return strategy;

			if (TupleInfo.IsTupleType(type))
				return DbValueTypeStrategy.Tuple;

			if (type.GetTypeInfo().IsEnum)
				return DbValueTypeStrategy.Enum;

			return DbValueTypeStrategy.DtoProperties;
		}

		private static IDbValueTypeInfo CreateInfo(Type type) =>
			(IDbValueTypeInfo) typeof(DbValueTypeInfo<>).MakeGenericType(type).GetTypeInfo().GetDeclaredField("Instance").GetValue(null);

		private static readonly ConcurrentDictionary<Type, IDbValueTypeInfo> s_infos = new ConcurrentDictionary<Type, IDbValueTypeInfo>();

		private static readonly Dictionary<Type, DbValueTypeStrategy> s_strategies = new Dictionary<Type, DbValueTypeStrategy>
		{
			[typeof(string)] = DbValueTypeStrategy.CastValue,
			[typeof(long)] = DbValueTypeStrategy.CastValue,
			[typeof(int)] = DbValueTypeStrategy.CastValue,
			[typeof(short)] = DbValueTypeStrategy.CastValue,
			[typeof(byte)] = DbValueTypeStrategy.CastValue,
			[typeof(ulong)] = DbValueTypeStrategy.CastValue,
			[typeof(uint)] = DbValueTypeStrategy.CastValue,
			[typeof(ushort)] = DbValueTypeStrategy.CastValue,
			[typeof(sbyte)] = DbValueTypeStrategy.CastValue,
			[typeof(double)] = DbValueTypeStrategy.CastValue,
			[typeof(float)] = DbValueTypeStrategy.CastValue,
			[typeof(decimal)] = DbValueTypeStrategy.CastValue,
			[typeof(bool)] = DbValueTypeStrategy.CastValue,
			[typeof(Guid)] = DbValueTypeStrategy.CastValue,
			[typeof(DateTime)] = DbValueTypeStrategy.CastValue,
			[typeof(DateTimeOffset)] = DbValueTypeStrategy.CastValue,
			[typeof(TimeSpan)] = DbValueTypeStrategy.CastValue,
			[typeof(byte[])] = DbValueTypeStrategy.ByteArray,
			[typeof(object)] = DbValueTypeStrategy.Dynamic,
			[typeof(Dictionary<string, object?>)] = DbValueTypeStrategy.Dictionary,
			[typeof(IDictionary<string, object?>)] = DbValueTypeStrategy.Dictionary,
			[typeof(IReadOnlyDictionary<string, object?>)] = DbValueTypeStrategy.Dictionary,
			[typeof(IDictionary)] = DbValueTypeStrategy.Dictionary,
		};
	}

	internal sealed class DbValueTypeInfo<T> : IDbValueTypeInfo
	{
		public Type Type => m_nullableType ?? m_coreType;

		public int? FieldCount { get; }

		public T GetValue(IDataRecord record, int index, int count)
		{
			if (FieldCount != null && FieldCount.Value != count)
				throw new InvalidOperationException($"Type must be read from {FieldCount.Value} fields but is being read from {count} fields: {Type.FullName}");

			if (m_strategy == DbValueTypeStrategy.DtoProperties)
			{
				T dto = DtoInfo.GetInfo<T>().CreateNew();
				bool notNull = false;
				for (int i = index; i < index + count; i++)
				{
					string name = record.GetName(i);
					if (!m_properties!.TryGetValue(NormalizeFieldName(name), out var property))
						throw new InvalidOperationException($"Type does not have a property for '{name}': {Type.FullName}");
					if (!record.IsDBNull(i))
					{
						property.Dto.SetValue(dto, property.Db.GetValue(record, i, 1));
						notNull = true;
					}
				}
				return notNull ? dto : default;
			}
			else if (m_strategy == DbValueTypeStrategy.Dynamic && count > 1)
			{
				IDictionary<string, object?> obj = new ExpandoObject();
				bool notNull = false;
				for (int i = index; i < index + count; i++)
				{
					string name = record.GetName(i);
					if (!record.IsDBNull(i))
					{
						obj[name] = record.GetValue(i);
						notNull = true;
					}
					else
					{
						obj[name] = null;
					}
				}
				return notNull ? (T) obj : default;
			}
			else if (m_strategy == DbValueTypeStrategy.ByteArray)
			{
				if (record.IsDBNull(index))
					return default!;

				int byteCount = (int) record.GetBytes(index, 0, null, 0, 0);
				byte[] bytes = new byte[byteCount];
				record.GetBytes(index, 0, bytes, 0, byteCount);
				return (T) (object) bytes;
			}
			else if (m_strategy == DbValueTypeStrategy.Tuple)
			{
				int valueCount = m_tupleTypeInfos!.Count;
				object?[] values = new object[valueCount];
				int recordIndex = index;
				for (int valueIndex = 0; valueIndex < valueCount; valueIndex++)
				{
					var info = m_tupleTypeInfos[valueIndex];

					int fieldCount;
					int? nullIndex = null;
					if (info.FieldCount == null)
					{
						int? remainingFieldCount = 0;
						int minimumRemainingFieldCount = 0;
						for (int nextValueIndex = valueIndex + 1; nextValueIndex < valueCount; nextValueIndex++)
						{
							int? nextFieldCount = m_tupleTypeInfos[nextValueIndex].FieldCount;
							if (nextFieldCount != null)
							{
								remainingFieldCount += nextFieldCount.Value;
								minimumRemainingFieldCount += nextFieldCount.Value;
							}
							else
							{
								remainingFieldCount = null;
								minimumRemainingFieldCount += 1;
							}
						}

						if (remainingFieldCount != null)
						{
							fieldCount = count - recordIndex - remainingFieldCount.Value;
						}
						else
						{
							for (int nextRecordIndex = recordIndex + 1; nextRecordIndex < count; nextRecordIndex++)
							{
								if (record.GetName(nextRecordIndex).Equals("NULL", StringComparison.OrdinalIgnoreCase))
								{
									nullIndex = nextRecordIndex;
									break;
								}
							}

							if (nullIndex != null)
							{
								fieldCount = nullIndex.Value - recordIndex;
							}
							else if (count - (recordIndex + 1) == minimumRemainingFieldCount)
							{
								fieldCount = 1;
							}
							else
							{
								throw new InvalidOperationException($"Tuple item {valueIndex} must be terminated by a field named 'NULL': {Type.FullName}");
							}
						}
					}
					else
					{
						fieldCount = info.FieldCount.Value;
					}

					values[valueIndex] = info.GetValue(record, recordIndex, fieldCount);
					recordIndex = nullIndex + 1 ?? recordIndex + fieldCount;
				}

				return m_tupleInfo!.CreateNew(values);
			}
			else if (m_strategy == DbValueTypeStrategy.CastValue || m_strategy == DbValueTypeStrategy.Enum || m_strategy == DbValueTypeStrategy.Dynamic)
			{
				object value = record.GetValue(index);
				if (value == DBNull.Value)
				{
					if (m_nullableType == null)
						throw new InvalidOperationException($"Failed to cast null to {Type.FullName}.");
					return default!;
				}

				try
				{
					if (m_strategy == DbValueTypeStrategy.Enum)
						return (T) Enum.ToObject(m_coreType, value);
					else
						return (T) value;
				}
				catch (Exception exception) when (exception is ArgumentException || exception is InvalidCastException)
				{
					throw new InvalidOperationException($"Failed to cast {value?.GetType().FullName} to {Type.FullName}.", exception);
				}
			}
			else if (m_strategy == DbValueTypeStrategy.Dictionary)
			{
				var dictionary = new Dictionary<string, object?>();
				bool notNull = false;
				for (int i = index; i < index + count; i++)
				{
					string name = record.GetName(i);
					if (!record.IsDBNull(i))
					{
						dictionary[name] = record.GetValue(i);
						notNull = true;
					}
					else
					{
						dictionary[name] = null;
					}
				}
				return notNull ? (T) (object) dictionary : default;
			}
			else
			{
				throw new InvalidOperationException($"Unknown strategy: {m_strategy}");
			}
		}

		internal static readonly DbValueTypeInfo<T> Instance = new DbValueTypeInfo<T>();

		private DbValueTypeInfo()
		{
			var type = typeof(T);
			if (type.IsValueType)
			{
				var underlyingType = Nullable.GetUnderlyingType(type);
				m_coreType = underlyingType ?? type;
				m_nullableType = underlyingType != null ? type : null;
			}
			else
			{
				m_coreType = type;
				m_nullableType = type;
			}

			m_strategy = DbValueTypeInfo.GetStrategy(m_coreType);
			if (m_strategy == DbValueTypeStrategy.DtoProperties)
			{
				m_properties = DtoInfo.GetInfo<T>().Properties.ToDictionary(
					x => NormalizeFieldName(x.Name),
					x => (x, DbValueTypeInfo.GetInfo(x.ValueType)),
					StringComparer.OrdinalIgnoreCase);
			}
			else if (m_strategy == DbValueTypeStrategy.Tuple)
			{
				m_tupleInfo = TupleInfo.GetInfo<T>();
				m_tupleTypeInfos = m_tupleInfo.ItemTypes.Select(DbValueTypeInfo.GetInfo).ToList();
				FieldCount = m_tupleTypeInfos.Aggregate((int?) 0, (x, y) => x + y.FieldCount);
			}
			else if (m_strategy != DbValueTypeStrategy.Dynamic && m_strategy != DbValueTypeStrategy.Dictionary)
			{
				FieldCount = 1;
			}
		}

		object? IDbValueTypeInfo.GetValue(IDataRecord record, int index, int count) => GetValue(record, index, count);

		private static string NormalizeFieldName(string text) => text.Replace("_", "");

		private readonly Type m_coreType;
		private readonly Type? m_nullableType;
		private readonly DbValueTypeStrategy m_strategy;
		private readonly Dictionary<string, (IDtoProperty<T> Dto, IDbValueTypeInfo Db)>? m_properties;
		private readonly TupleInfo<T>? m_tupleInfo;
		private readonly IReadOnlyList<IDbValueTypeInfo>? m_tupleTypeInfos;
	}
}
