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
}
