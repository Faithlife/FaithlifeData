using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Equivalency;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class DataRecordExtensionsTests
	{
		[Test]
		public void Strings()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			reader.Get<string>(0, 1).Should().Be(s_dto.TheString);

			// get nulls
			reader.Read().Should().BeTrue();

			reader.Get<string>(0, 1).Should().BeNull();
		}

		[Test]
		public void NonNullableScalars()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			reader.Get<int>(1, 1).Should().Be(s_dto.TheInt32);
			reader.Get<long>(2, 1).Should().Be(s_dto.TheInt64);
			reader.Get<bool>(3, 1).Should().Be(s_dto.TheBool);
			reader.Get<float>(4, 1).Should().Be(s_dto.TheSingle);
			reader.Get<double>(5, 1).Should().Be(s_dto.TheDouble);

			// get nulls
			reader.Read().Should().BeTrue();

			Invoking(() => reader.Get<int>(1, 1)).Should().Throw<InvalidOperationException>();
			Invoking(() => reader.Get<long>(2, 1)).Should().Throw<InvalidOperationException>();
			Invoking(() => reader.Get<bool>(3, 1)).Should().Throw<InvalidOperationException>();
			Invoking(() => reader.Get<float>(4, 1)).Should().Throw<InvalidOperationException>();
			Invoking(() => reader.Get<double>(5, 1)).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void NullableScalars()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			reader.Get<int?>(1, 1).Should().Be(s_dto.TheInt32);
			reader.Get<long?>(2, 1).Should().Be(s_dto.TheInt64);
			reader.Get<bool?>(3, 1).Should().Be(s_dto.TheBool);
			reader.Get<float?>(4, 1).Should().Be(s_dto.TheSingle);
			reader.Get<double?>(5, 1).Should().Be(s_dto.TheDouble);

			// get nulls
			reader.Read().Should().BeTrue();

			reader.Get<int?>(1, 1).Should().BeNull();
			reader.Get<long?>(2, 1).Should().BeNull();
			reader.Get<bool?>(3, 1).Should().BeNull();
			reader.Get<float?>(4, 1).Should().BeNull();
			reader.Get<double?>(5, 1).Should().BeNull();
		}

		[Test]
		public void Enums()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			reader.Get<Answer>(1, 1).Should().Be(Answer.FortyTwo);
			reader.Get<Answer>(2, 1).Should().Be(Answer.FortyTwo);
			reader.Get<Answer?>(1, 1).Should().Be(Answer.FortyTwo);
			reader.Get<Answer?>(2, 1).Should().Be(Answer.FortyTwo);

			// get nulls
			reader.Read().Should().BeTrue();

			Invoking(() => reader.Get<Answer>(1, 1)).Should().Throw<InvalidOperationException>();
			Invoking(() => reader.Get<Answer>(2, 1)).Should().Throw<InvalidOperationException>();
			reader.Get<Answer?>(1, 1).Should().BeNull();
			reader.Get<Answer?>(2, 1).Should().BeNull();
		}

		[Test]
		public void BadIndexCount()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			reader.Read().Should().BeTrue();

			Invoking(() => reader.Get<ItemDto>(-1, 2)).Should().Throw<ArgumentException>();
			Invoking(() => reader.Get<ItemDto>(2, -1)).Should().Throw<ArgumentException>();
			Invoking(() => reader.Get<ItemDto>(7, 1)).Should().Throw<ArgumentException>();
			Invoking(() => reader.Get<ItemDto>(8, 0)).Should().Throw<ArgumentException>();
		}

		[Test]
		public void BadCast()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			reader.Read().Should().BeTrue();

			Invoking(() => reader.Get<long>(1, 1).Should().Be(42)).Should().Throw<InvalidOperationException>();
			Invoking(() => reader.Get<Answer>(0, 1).Should().Be(42)).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void BadFieldCount()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			reader.Read().Should().BeTrue();

			Invoking(() => reader.Get<(string, int)>(0, 1)).Should().Throw<InvalidOperationException>();
			reader.Get<(string?, int)>(0, 2).Should().Be((s_dto.TheString, s_dto.TheInt32));
			Invoking(() => reader.Get<(string, int)>(0, 3)).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void ByteArrayTests()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			reader.Get<byte[]>(6, 1).Should().Equal(s_dto.TheBlob);

			// get nulls
			reader.Read().Should().BeTrue();

			reader.Get<byte[]>(6, 1).Should().BeNull();
		}

		[Test]
		public void StreamTests()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			var bytes = new byte[100];
			using (var stream = reader.Get<Stream>(6, 1))
				stream.Read(bytes, 0, bytes.Length).Should().Be(s_dto.TheBlob!.Length);
			bytes.Take(s_dto.TheBlob!.Length).Should().Equal(s_dto.TheBlob);

			// get nulls
			reader.Read().Should().BeTrue();

			reader.Get<Stream>(6, 1).Should().BeNull();
		}

		[Test]
		public void TupleTests()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			reader.Get<(string?, int, long, bool, float, double)>(0, 6)
				.Should().Be((s_dto.TheString, s_dto.TheInt32, s_dto.TheInt64, s_dto.TheBool, s_dto.TheSingle, s_dto.TheDouble));
			reader.Get<(string?, int, long, bool, float, double)>(..^1)
				.Should().Be((s_dto.TheString, s_dto.TheInt32, s_dto.TheInt64, s_dto.TheBool, s_dto.TheSingle, s_dto.TheDouble));

			// get nulls
			reader.Read().Should().BeTrue();

			reader.Get<(string?, int?, long?, bool?, float?, double?)>(0, 6)
				.Should().Be((null, null, null, null, null, null));
			reader.Get<(string?, int?, long?, bool?, float?, double?)>(..6)
				.Should().Be((null, null, null, null, null, null));
		}

		[Test]
		public void DtoTests()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			// DTO
			reader.Get<ItemDto>(0, 7).Should().BeEquivalentTo(s_dto);
			reader.Get<ItemDto>(0, 1).Should().BeEquivalentTo(new ItemDto { TheString = s_dto.TheString });
			reader.Get<ItemDto>(0, 0).Should().BeNull();
			reader.Get<ItemDto>(7, 0).Should().BeNull();

			// tuple with DTO
			var tuple = reader.Get<(string, ItemDto, bool)>(0, 4);
			tuple.Item1.Should().Be(s_dto.TheString);
			tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheInt32 = s_dto.TheInt32, TheInt64 = s_dto.TheInt64 });
			tuple.Item3.Should().BeTrue();

			// tuple with two DTOs (needs NULL terminator)
			Invoking(() => reader.Get<(ItemDto, ItemDto)>(0, 3)).Should().Throw<InvalidOperationException>();

			// get nulls
			reader.Read().Should().BeTrue();

			// all nulls returns null DTO
			reader.Get<ItemDto>(0, 7).Should().BeNull();
		}

		[Test]
		public void TwoDtos()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt64, null, TheBool, TheDouble from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			// two DTOs
			var tuple = reader.Get<(ItemDto, ItemDto)>(0, 5);
			tuple.Item1.Should().BeEquivalentTo(new ItemDto { TheString = s_dto.TheString, TheInt64 = s_dto.TheInt64 });
			tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheBool = s_dto.TheBool, TheDouble = s_dto.TheDouble });

			// get nulls
			reader.Read().Should().BeTrue();

			// two DTOs
			tuple = reader.Get<(ItemDto, ItemDto)>(0, 5);
			tuple.Item1.Should().BeNull();
			tuple.Item2.Should().BeNull();
		}

		[Test]
		public void TwoOneFieldDtos()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt64 from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			// two DTOs
			var tuple = reader.Get<(ItemDto, ItemDto)>(0, 2);
			tuple.Item1.Should().BeEquivalentTo(new ItemDto { TheString = s_dto.TheString });
			tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheInt64 = s_dto.TheInt64 });

			// get nulls
			reader.Read().Should().BeTrue();

			// two DTOs
			tuple = reader.Get<(ItemDto, ItemDto)>(0, 2);
			tuple.Item1.Should().BeNull();
			tuple.Item2.Should().BeNull();
		}

#if NET5_0
		[Test]
		[TestCase(typeof(ItemRecord), true)]
		[TestCase(typeof(ItemDto), false)]
		public void IsRecordLikeTests(Type type, bool isRecordLike)
		{
			Assert.AreEqual(isRecordLike, DbValueTypeInfo.IsRecordLike(type));
		}

		[Test]
		public void RecordTests()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			// compare by members because the TheBlob is compared by reference in .Equals()
			Func<EquivalencyAssertionOptions<ItemRecord>, EquivalencyAssertionOptions<ItemRecord>> configureEquivalency = options => options.ComparingByMembers<ItemRecord>();

			// record
			reader.Get<ItemRecord>(0, 7).Should().BeEquivalentTo(s_record, configureEquivalency);
			reader.Get<ItemRecord>(0, 1).Should().BeEquivalentTo(new ItemRecord(s_record.TheString, default, default, default, default, default, default), configureEquivalency);
			reader.Get<ItemRecord>(0, 0).Should().BeNull();
			reader.Get<ItemRecord>(7, 0).Should().BeNull();

			// tuple with record
			var tuple = reader.Get<(string, ItemRecord, bool)>(0, 4);
			tuple.Item1.Should().Be(s_record.TheString);
			tuple.Item2.Should().BeEquivalentTo(new ItemRecord(default, s_record.TheInt32, s_record.TheInt64, default, default, default, default), configureEquivalency);
			tuple.Item3.Should().BeTrue();

			// tuple with two records (needs NULL terminator)
			Invoking(() => reader.Get<(ItemRecord, ItemRecord)>(0, 3)).Should().Throw<InvalidOperationException>();

			// get nulls
			reader.Read().Should().BeTrue();

			// all nulls returns null record
			reader.Get<ItemRecord>(0, 7).Should().BeNull();
		}
#endif

		[Test]
		public void CaseInsensitivePropertyName()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select thestring, THEint64 from items;";
			using var reader = command.ExecuteReader();

			reader.Read().Should().BeTrue();
			reader.Get<ItemDto>(0, 2)
				.Should().BeEquivalentTo(new ItemDto { TheString = s_dto.TheString, TheInt64 = s_dto.TheInt64 });
		}

		[Test]
		public void UnderscorePropertyName()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString as the_string, TheInt64 as the_int64 from items;";
			using var reader = command.ExecuteReader();

			reader.Read().Should().BeTrue();
			reader.Get<ItemDto>(0, 2)
				.Should().BeEquivalentTo(new ItemDto { TheString = s_dto.TheString, TheInt64 = s_dto.TheInt64 });
		}

		[Test]
		public void BadPropertyName()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt64 as Nope from items;";
			using var reader = command.ExecuteReader();

			reader.Read().Should().BeTrue();
			Invoking(() => reader.Get<ItemDto>(0, 2)).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void DynamicTests()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			// dynamic
			((string) reader.Get<dynamic>(0, 7).TheString).Should().Be(s_dto.TheString);
			((bool) ((dynamic) reader.Get<object>(0, 7)).TheBool).Should().Be(s_dto.TheBool);

			// tuple with dynamic
			var tuple = reader.Get<(string, dynamic, bool)>(0, 4);
			tuple.Item1.Should().Be(s_dto.TheString);
			((long) tuple.Item2.TheInt64).Should().Be(s_dto.TheInt64);
			tuple.Item3.Should().BeTrue();

			// tuple with two dynamics (needs NULL terminator)
			Invoking(() => reader.Get<(dynamic, dynamic)>(0, 3)).Should().Throw<InvalidOperationException>();

			// get nulls
			reader.Read().Should().BeTrue();

			// all nulls returns null dynamic
			((object) reader.Get<dynamic>(0, 7)).Should().BeNull();
			reader.Get<object>(0, 7).Should().BeNull();
		}

		[Test]
		public void DictionaryTests()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			// dictionary
			((string) reader.Get<Dictionary<string, object?>>(0, 7)["TheString"]!).Should().Be(s_dto.TheString);
			((int) reader.Get<IDictionary<string, object?>>(0, 7)["TheInt32"]!).Should().Be(s_dto.TheInt32);
			((long) reader.Get<IReadOnlyDictionary<string, object?>>(0, 7)["TheInt64"]!).Should().Be(s_dto.TheInt64);
			((bool) reader.Get<IDictionary>(0, 7)["TheBool"]!).Should().Be(s_dto.TheBool);

			// get nulls
			reader.Read().Should().BeTrue();

			// all nulls returns null dictionary
			reader.Get<IDictionary>(0, 7).Should().BeNull();
		}

		[Test]
		public void ObjectTests()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			// get non-nulls
			reader.Read().Should().BeTrue();

			// object/dynamic
			reader.Get<object>(0).Should().Be(s_dto.TheString);
			((bool) reader.Get<dynamic>(3)).Should().Be(s_dto.TheBool);

			// tuple with object
			var tuple = reader.Get<(string, object, long)>(0, 3);
			tuple.Item1.Should().Be(s_dto.TheString);
			tuple.Item2.Should().Be(s_dto.TheInt32);
			tuple.Item3.Should().Be(s_dto.TheInt64);

			// tuple with three objects (doesn't need NULL terminator when the field count matches exactly)
			var tuple2 = reader.Get<(object, object, object)>(0, 3);
			tuple2.Item1.Should().Be(s_dto.TheString);
			tuple2.Item2.Should().Be(s_dto.TheInt32);
			tuple2.Item3.Should().Be(s_dto.TheInt64);

			// get nulls
			reader.Read().Should().BeTrue();

			// all nulls returns null dynamic
			reader.Get<object>(0).Should().BeNull();
			((object) reader.Get<dynamic>(0)).Should().BeNull();
		}

		[Test]
		public void GetExtension()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			reader.Read().Should().BeTrue();
			reader.Get<ItemDto>().Should().BeEquivalentTo(s_dto);
		}

		[Test]
		public void GetAtExtension()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			reader.Read().Should().BeTrue();
			reader.Get<long>(2).Should().Be(s_dto.TheInt64);
			reader.Get<long>("TheInt64").Should().Be(s_dto.TheInt64);
			reader.Get<long>(^5).Should().Be(s_dto.TheInt64);
		}

		[Test]
		public void GetRangeExtension()
		{
			using var connection = GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
			using var reader = command.ExecuteReader();

			reader.Read().Should().BeTrue();
			reader.Get<(long, bool)>(2, 2).Should().Be((s_dto.TheInt64, s_dto.TheBool));
			reader.Get<(long, bool)>(2..4).Should().Be((s_dto.TheInt64, s_dto.TheBool));
			reader.Get<(long, bool)>("TheInt64", 2).Should().Be((s_dto.TheInt64, s_dto.TheBool));
			reader.Get<(long, bool)>("TheInt64", "TheBool").Should().Be((s_dto.TheInt64, s_dto.TheBool));
		}

		private static IDbConnection GetOpenConnection()
		{
			var connection = new SQLiteConnection("Data Source=:memory:");
			connection.Open();

			using (var command = connection.CreateCommand())
			{
				command.CommandText = "create table Items (TheString text null, TheInt32 int null, TheInt64 bigint null, TheBool bool null, TheSingle single null, TheDouble double null, TheBlob blob null);";
				command.ExecuteNonQuery();
			}

			using (var command = connection.CreateCommand())
			{
				command.CommandText = "insert into Items (TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob) values ('hey', 42, 42, 1, 3.14, 3.1415, X'01FE');";
				command.ExecuteNonQuery();
			}

			using (var command = connection.CreateCommand())
			{
				command.CommandText = "insert into Items (TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob) values (null, null, null, null, null, null, null);";
				command.ExecuteNonQuery();
			}

			return connection;
		}

		private class ItemDto
		{
			public string? TheString { get; set; }
			public int TheInt32 { get; set; }
			public long TheInt64 { get; set; }
			public bool TheBool { get; set; }
			public float TheSingle { get; set; }
			public double TheDouble { get; set; }
			public byte[]? TheBlob { get; set; }
		}

#if NET5_0
#pragma warning disable SA1313
		private record ItemRecord(string? TheString, int TheInt32, long TheInt64, bool TheBool, float TheSingle, double TheDouble, byte[]? TheBlob);
#pragma warning restore SA1313
#endif

		private enum Answer
		{
			FortyTwo = 42,
		}

		private static readonly ItemDto s_dto = new ItemDto
		{
			TheString = "hey",
			TheInt32 = 42,
			TheInt64 = 42,
			TheBool = true,
			TheSingle = 3.14f,
			TheDouble = 3.1415,
			TheBlob = new byte[] { 0x01, 0xFE },
		};

#if NET5_0
		private static readonly ItemRecord s_record = new ItemRecord(
			TheString: "hey",
			TheInt32: 42,
			TheInt64: 42,
			TheBool: true,
			TheSingle: 3.14f,
			TheDouble: 3.1415,
			TheBlob: new byte[] { 0x01, 0xFE });
#endif
	}
}
