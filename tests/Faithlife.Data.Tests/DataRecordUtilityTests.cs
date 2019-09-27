using System;
using System.Data;
using System.Data.SQLite;
using FluentAssertions;
using NUnit.Framework;
using static Faithlife.Data.Tests.FluentAction;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class DataRecordUtilityTests
	{
		[Test]
		public void Strings()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<string>(reader, 0, 1).Should().Be(s_record.TheString);

					// get nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<string>(reader, 0, 1).Should().BeNull();
				}
			}
		}

		[Test]
		public void StringsByType()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue(typeof(string), reader, 0, 1).Should().Be(s_record.TheString);

					// get nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue(typeof(string), reader, 0, 1).Should().BeNull();
				}
			}
		}

		[Test]
		public void NonNullableScalars()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<int>(reader, 1, 1).Should().Be(s_record.TheInt32);
					DataRecordUtility.GetValue<long>(reader, 2, 1).Should().Be(s_record.TheInt64);
					DataRecordUtility.GetValue<bool>(reader, 3, 1).Should().Be(s_record.TheBool);
					DataRecordUtility.GetValue<float>(reader, 4, 1).Should().Be(s_record.TheSingle);
					DataRecordUtility.GetValue<double>(reader, 5, 1).Should().Be(s_record.TheDouble);

					// get nulls
					reader.Read().Should().BeTrue();

					Invoking(() => DataRecordUtility.GetValue<int>(reader, 1, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DataRecordUtility.GetValue<long>(reader, 2, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DataRecordUtility.GetValue<bool>(reader, 3, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DataRecordUtility.GetValue<float>(reader, 4, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DataRecordUtility.GetValue<double>(reader, 5, 1)).Should().Throw<InvalidOperationException>();
				}
			}
		}

		[Test]
		public void NullableScalars()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<int?>(reader, 1, 1).Should().Be(s_record.TheInt32);
					DataRecordUtility.GetValue<long?>(reader, 2, 1).Should().Be(s_record.TheInt64);
					DataRecordUtility.GetValue<bool?>(reader, 3, 1).Should().Be(s_record.TheBool);
					DataRecordUtility.GetValue<float?>(reader, 4, 1).Should().Be(s_record.TheSingle);
					DataRecordUtility.GetValue<double?>(reader, 5, 1).Should().Be(s_record.TheDouble);

					// get nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<int?>(reader, 1, 1).Should().BeNull();
					DataRecordUtility.GetValue<long?>(reader, 2, 1).Should().BeNull();
					DataRecordUtility.GetValue<bool?>(reader, 3, 1).Should().BeNull();
					DataRecordUtility.GetValue<float?>(reader, 4, 1).Should().BeNull();
					DataRecordUtility.GetValue<double?>(reader, 5, 1).Should().BeNull();
				}
			}
		}

		[Test]
		public void Enums()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<Answer>(reader, 1, 1).Should().Be(Answer.FortyTwo);
					DataRecordUtility.GetValue<Answer>(reader, 2, 1).Should().Be(Answer.FortyTwo);
					DataRecordUtility.GetValue<Answer?>(reader, 1, 1).Should().Be(Answer.FortyTwo);
					DataRecordUtility.GetValue<Answer?>(reader, 2, 1).Should().Be(Answer.FortyTwo);

					// get nulls
					reader.Read().Should().BeTrue();

					Invoking(() => DataRecordUtility.GetValue<Answer>(reader, 1, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DataRecordUtility.GetValue<Answer>(reader, 2, 1)).Should().Throw<InvalidOperationException>();
					DataRecordUtility.GetValue<Answer?>(reader, 1, 1).Should().BeNull();
					DataRecordUtility.GetValue<Answer?>(reader, 2, 1).Should().BeNull();
				}
			}
		}

		[Test]
		public void BadIndexCount()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().Should().BeTrue();

					Invoking(() => DataRecordUtility.GetValue<ItemRecord>(reader, -1, 2)).Should().Throw<ArgumentException>();
					Invoking(() => DataRecordUtility.GetValue<ItemRecord>(reader, 2, -1)).Should().Throw<ArgumentException>();
					Invoking(() => DataRecordUtility.GetValue<ItemRecord>(reader, 7, 1)).Should().Throw<ArgumentException>();
					Invoking(() => DataRecordUtility.GetValue<ItemRecord>(reader, 8, 0)).Should().Throw<ArgumentException>();
				}
			}
		}

		[Test]
		public void BadCast()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().Should().BeTrue();

					Invoking(() => DataRecordUtility.GetValue<long>(reader, 1, 1).Should().Be(42)).Should().Throw<InvalidOperationException>();
					Invoking(() => DataRecordUtility.GetValue<Answer>(reader, 0, 1).Should().Be(42)).Should().Throw<InvalidOperationException>();
				}
			}
		}

		[Test]
		public void BadFieldCount()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().Should().BeTrue();

					Invoking(() => DataRecordUtility.GetValue<(string, int)>(reader, 0, 1)).Should().Throw<InvalidOperationException>();
					DataRecordUtility.GetValue<(string, int)>(reader, 0, 2).Should().Be((s_record.TheString, s_record.TheInt32));
					Invoking(() => DataRecordUtility.GetValue<(string, int)>(reader, 0, 3)).Should().Throw<InvalidOperationException>();
				}
			}
		}

		[Test]
		public void ByteArrayTests()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<byte[]>(reader, 6, 1).Should().Equal(s_record.TheBlob);

					// get nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<byte[]>(reader, 6, 1).Should().BeNull();
				}
			}
		}

		[Test]
		public void TupleTests()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<(string, int, long, bool, float, double)>(reader, 0, 6)
						.Should().Be((s_record.TheString, s_record.TheInt32, s_record.TheInt64, s_record.TheBool, s_record.TheSingle, s_record.TheDouble));

					// get nulls
					reader.Read().Should().BeTrue();

					DataRecordUtility.GetValue<(string, int?, long?, bool?, float?, double?)>(reader, 0, 6)
						.Should().Be((null, null, null, null, null, null));
				}
			}
		}

		[Test]
		public void DtoTests()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					// DTO
					DataRecordUtility.GetValue<ItemRecord>(reader, 0, 7).Should().BeEquivalentTo(s_record);
					DataRecordUtility.GetValue<ItemRecord>(reader, 0, 1).Should().BeEquivalentTo(new ItemRecord { TheString = s_record.TheString });
					DataRecordUtility.GetValue<ItemRecord>(reader, 0, 0).Should().BeNull();
					DataRecordUtility.GetValue<ItemRecord>(reader, 7, 0).Should().BeNull();

					// tuple with DTO
					var tuple = DataRecordUtility.GetValue<(string, ItemRecord, bool)>(reader, 0, 4);
					tuple.Item1.Should().Be(s_record.TheString);
					tuple.Item2.Should().BeEquivalentTo(new ItemRecord { TheInt32 = s_record.TheInt32, TheInt64 = s_record.TheInt64 });
					tuple.Item3.Should().BeTrue();

					// tuple with two DTOs (needs NULL terminator)
					Invoking(() => DataRecordUtility.GetValue<(ItemRecord, ItemRecord)>(reader, 0, 3)).Should().Throw<InvalidOperationException>();

					// get nulls
					reader.Read().Should().BeTrue();

					// all nulls returns null DTO
					DataRecordUtility.GetValue<ItemRecord>(reader, 0, 7).Should().BeNull();
				}
			}
		}

		[Test]
		public void TwoDtos()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt64, null, TheBool, TheDouble from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					// two DTOs
					var tuple = DataRecordUtility.GetValue<(ItemRecord, ItemRecord)>(reader, 0, 5);
					tuple.Item1.Should().BeEquivalentTo(new ItemRecord { TheString = s_record.TheString, TheInt64 = s_record.TheInt64 });
					tuple.Item2.Should().BeEquivalentTo(new ItemRecord { TheBool = s_record.TheBool, TheDouble = s_record.TheDouble });

					// get nulls
					reader.Read().Should().BeTrue();

					// two DTOs
					tuple = DataRecordUtility.GetValue<(ItemRecord, ItemRecord)>(reader, 0, 5);
					tuple.Item1.Should().BeNull();
					tuple.Item2.Should().BeNull();
				}
			}
		}

		[Test]
		public void CaseInsensitivePropertyName()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select thestring, THEint64 from items;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().Should().BeTrue();
					DataRecordUtility.GetValue<ItemRecord>(reader, 0, 2)
						.Should().BeEquivalentTo(new ItemRecord { TheString = s_record.TheString, TheInt64 = s_record.TheInt64 });
				}
			}
		}

		[Test]
		public void BadPropertyName()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt64 as Nope from items;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().Should().BeTrue();
					Invoking(() => DataRecordUtility.GetValue<ItemRecord>(reader, 0, 2)).Should().Throw<InvalidOperationException>();
				}
			}
		}

		[Test]
		public void DynamicTests()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					// dynamic
					((string) DataRecordUtility.GetValue<dynamic>(reader, 0, 7).TheString).Should().Be(s_record.TheString);
					((bool) ((dynamic) DataRecordUtility.GetValue<object>(reader, 0, 7)).TheBool).Should().Be(s_record.TheBool);

					// tuple with dynamic
					var tuple = DataRecordUtility.GetValue<(string, dynamic, bool)>(reader, 0, 4);
					tuple.Item1.Should().Be(s_record.TheString);
					((long) tuple.Item2.TheInt64).Should().Be(s_record.TheInt64);
					tuple.Item3.Should().BeTrue();

					// tuple with two dynamics (needs NULL terminator)
					Invoking(() => DataRecordUtility.GetValue<(dynamic, dynamic)>(reader, 0, 3)).Should().Throw<InvalidOperationException>();

					// get nulls
					reader.Read().Should().BeTrue();

					// all nulls returns null dynamic
					((object) DataRecordUtility.GetValue<dynamic>(reader, 0, 7)).Should().BeNull();
					DataRecordUtility.GetValue<object>(reader, 0, 7).Should().BeNull();
				}
			}
		}

		[Test]
		public void GetExtension()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().Should().BeTrue();
					reader.Get<ItemRecord>().Should().BeEquivalentTo(s_record);
				}
			}
		}

		[Test]
		public void GetAtExtension()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().Should().BeTrue();
					reader.Get<long>(2).Should().Be(s_record.TheInt64);
					reader.Get<long>("TheInt64").Should().Be(s_record.TheInt64);
				}
			}
		}

		[Test]
		public void GetRangeExtension()
		{
			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().Should().BeTrue();
					reader.Get<(long, bool)>(2, 2).Should().Be((s_record.TheInt64, s_record.TheBool));
					reader.Get<(long, bool)>("TheInt64", 2).Should().Be((s_record.TheInt64, s_record.TheBool));
					reader.Get<(long, bool)>("TheInt64", "TheBool").Should().Be((s_record.TheInt64, s_record.TheBool));
				}
			}
		}

		private IDbConnection GetOpenConnection()
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

		private class ItemRecord
		{
			public string TheString { get; set; }
			public int TheInt32 { get; set; }
			public long TheInt64 { get; set; }
			public bool TheBool { get; set; }
			public float TheSingle { get; set; }
			public double TheDouble { get; set; }
			public byte[] TheBlob { get; set; }
		}

		private enum Answer
		{
			FortyTwo = 42,
		}

		private static readonly ItemRecord s_record = new ItemRecord
		{
			TheString = "hey",
			TheInt32 = 42,
			TheInt64 = 42,
			TheBool = true,
			TheSingle = 3.14f,
			TheDouble = 3.1415,
			TheBlob = new byte[] { 0x01, 0xFE },
		};
	}
}
