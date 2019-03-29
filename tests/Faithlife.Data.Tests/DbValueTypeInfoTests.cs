using System;
using System.Data;
using System.Data.SQLite;
using FluentAssertions;
using NUnit.Framework;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class DbValueTypeInfoTests
	{
		[Test]
		public void Strings()
		{
			var info = DbValueTypeInfo.GetInfo<string>();
			info.Type.Should().Be(typeof(string));
			info.FieldCount.Should().Be(1);

			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					info.GetValue(reader, 0, 1).Should().Be(s_record.TheString);

					// get nulls
					reader.Read().Should().BeTrue();

					info.GetValue(reader, 0, 1).Should().BeNull();
				}
			}
		}

		[Test]
		public void WeakStrings()
		{
			var info = DbValueTypeInfo.GetInfo(typeof(string));
			info.Type.Should().Be(typeof(string));
			info.FieldCount.Should().Be(1);

			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					info.GetValue(reader, 0, 1).Should().Be(s_record.TheString);

					// get nulls
					reader.Read().Should().BeTrue();

					info.GetValue(reader, 0, 1).Should().BeNull();
				}
			}
		}

		[Test]
		public void NonNullableScalars()
		{
			DbValueTypeInfo.GetInfo<int>().Type.Should().Be(typeof(int));

			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					DbValueTypeInfo.GetInfo<int>().GetValue(reader, 1, 1).Should().Be(s_record.TheInt32);
					DbValueTypeInfo.GetInfo<long>().GetValue(reader, 2, 1).Should().Be(s_record.TheInt64);
					DbValueTypeInfo.GetInfo<bool>().GetValue(reader, 3, 1).Should().Be(s_record.TheBool);
					DbValueTypeInfo.GetInfo<float>().GetValue(reader, 4, 1).Should().Be(s_record.TheSingle);
					DbValueTypeInfo.GetInfo<double>().GetValue(reader, 5, 1).Should().Be(s_record.TheDouble);

					// get nulls
					reader.Read().Should().BeTrue();

					Invoking(() => DbValueTypeInfo.GetInfo<int>().GetValue(reader, 1, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DbValueTypeInfo.GetInfo<long>().GetValue(reader, 2, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DbValueTypeInfo.GetInfo<bool>().GetValue(reader, 3, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DbValueTypeInfo.GetInfo<float>().GetValue(reader, 4, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DbValueTypeInfo.GetInfo<double>().GetValue(reader, 5, 1)).Should().Throw<InvalidOperationException>();
				}
			}
		}

		[Test]
		public void NullableScalars()
		{
			DbValueTypeInfo.GetInfo<int?>().Type.Should().Be(typeof(int?));

			using (var connection = GetOpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select TheString, TheInt32, TheInt64, TheBool, TheSingle, TheDouble, TheBlob from items;";
				using (var reader = command.ExecuteReader())
				{
					// get non-nulls
					reader.Read().Should().BeTrue();

					DbValueTypeInfo.GetInfo<int?>().GetValue(reader, 1, 1).Should().Be(s_record.TheInt32);
					DbValueTypeInfo.GetInfo<long?>().GetValue(reader, 2, 1).Should().Be(s_record.TheInt64);
					DbValueTypeInfo.GetInfo<bool?>().GetValue(reader, 3, 1).Should().Be(s_record.TheBool);
					DbValueTypeInfo.GetInfo<float?>().GetValue(reader, 4, 1).Should().Be(s_record.TheSingle);
					DbValueTypeInfo.GetInfo<double?>().GetValue(reader, 5, 1).Should().Be(s_record.TheDouble);

					// get nulls
					reader.Read().Should().BeTrue();

					DbValueTypeInfo.GetInfo<int?>().GetValue(reader, 1, 1).Should().BeNull();
					DbValueTypeInfo.GetInfo<long?>().GetValue(reader, 2, 1).Should().BeNull();
					DbValueTypeInfo.GetInfo<bool?>().GetValue(reader, 3, 1).Should().BeNull();
					DbValueTypeInfo.GetInfo<float?>().GetValue(reader, 4, 1).Should().BeNull();
					DbValueTypeInfo.GetInfo<double?>().GetValue(reader, 5, 1).Should().BeNull();
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

					DbValueTypeInfo.GetInfo<Answer>().GetValue(reader, 1, 1).Should().Be(Answer.FortyTwo);
					DbValueTypeInfo.GetInfo<Answer>().GetValue(reader, 2, 1).Should().Be(Answer.FortyTwo);
					DbValueTypeInfo.GetInfo<Answer?>().GetValue(reader, 1, 1).Should().Be(Answer.FortyTwo);
					DbValueTypeInfo.GetInfo<Answer?>().GetValue(reader, 2, 1).Should().Be(Answer.FortyTwo);

					// get nulls
					reader.Read().Should().BeTrue();

					Invoking(() => DbValueTypeInfo.GetInfo<Answer>().GetValue(reader, 1, 1)).Should().Throw<InvalidOperationException>();
					Invoking(() => DbValueTypeInfo.GetInfo<Answer>().GetValue(reader, 2, 1)).Should().Throw<InvalidOperationException>();
					DbValueTypeInfo.GetInfo<Answer?>().GetValue(reader, 1, 1).Should().BeNull();
					DbValueTypeInfo.GetInfo<Answer?>().GetValue(reader, 2, 1).Should().BeNull();
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

					Invoking(() => DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, -1, 2)).Should().Throw<ArgumentOutOfRangeException>();
					Invoking(() => DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 2, -1)).Should().Throw<ArgumentOutOfRangeException>();
					Invoking(() => DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 7, 1)).Should().Throw<ArgumentOutOfRangeException>();
					Invoking(() => DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 8, 0)).Should().Throw<ArgumentOutOfRangeException>();
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

					Invoking(() => DbValueTypeInfo.GetInfo<long>().GetValue(reader, 1, 1).Should().Be(42)).Should().Throw<InvalidOperationException>();
					Invoking(() => DbValueTypeInfo.GetInfo<Answer>().GetValue(reader, 0, 1).Should().Be(42)).Should().Throw<InvalidOperationException>();
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

					Invoking(() => DbValueTypeInfo.GetInfo<(string, int)>().GetValue(reader, 0, 1)).Should().Throw<InvalidOperationException>();
					DbValueTypeInfo.GetInfo<(string, int)>().GetValue(reader, 0, 2).Should().Be((s_record.TheString, s_record.TheInt32));
					Invoking(() => DbValueTypeInfo.GetInfo<(string, int)>().GetValue(reader, 0, 3)).Should().Throw<InvalidOperationException>();
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

					DbValueTypeInfo.GetInfo<byte[]>().GetValue(reader, 6, 1).Should().Equal(s_record.TheBlob);

					// get nulls
					reader.Read().Should().BeTrue();

					DbValueTypeInfo.GetInfo<byte[]>().GetValue(reader, 6, 1).Should().BeNull();
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

					DbValueTypeInfo.GetInfo<(string, int, long, bool, float, double)>().GetValue(reader, 0, 6)
						.Should().Be((s_record.TheString, s_record.TheInt32, s_record.TheInt64, s_record.TheBool, s_record.TheSingle, s_record.TheDouble));

					// get nulls
					reader.Read().Should().BeTrue();

					DbValueTypeInfo.GetInfo<(string, int?, long?, bool?, float?, double?)>().GetValue(reader, 0, 6)
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
					DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 0, 7).Should().BeEquivalentTo(s_record);
					DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 0, 1).Should().BeEquivalentTo(new ItemRecord { TheString = s_record.TheString });
					DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 0, 0).Should().BeNull();
					DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 7, 0).Should().BeNull();

					// tuple with DTO
					var tuple = DbValueTypeInfo.GetInfo<(string, ItemRecord, bool)>().GetValue(reader, 0, 4);
					tuple.Item1.Should().Be(s_record.TheString);
					tuple.Item2.Should().BeEquivalentTo(new ItemRecord { TheInt32 = s_record.TheInt32, TheInt64 = s_record.TheInt64 });
					tuple.Item3.Should().BeTrue();

					// tuple with two DTOs (needs NULL terminator)
					Invoking(() => DbValueTypeInfo.GetInfo<(ItemRecord, ItemRecord)>().GetValue(reader, 0, 3)).Should().Throw<InvalidOperationException>();

					// get nulls
					reader.Read().Should().BeTrue();

					// all nulls returns null DTO
					DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 0, 7).Should().BeNull();
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
					var tuple = DbValueTypeInfo.GetInfo<(ItemRecord, ItemRecord)>().GetValue(reader, 0, 5);
					tuple.Item1.Should().BeEquivalentTo(new ItemRecord { TheString = s_record.TheString, TheInt64 = s_record.TheInt64 });
					tuple.Item2.Should().BeEquivalentTo(new ItemRecord { TheBool = s_record.TheBool, TheDouble = s_record.TheDouble });

					// get nulls
					reader.Read().Should().BeTrue();

					// two DTOs
					tuple = DbValueTypeInfo.GetInfo<(ItemRecord, ItemRecord)>().GetValue(reader, 0, 5);
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
					DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 0, 2)
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
					Invoking(() => DbValueTypeInfo.GetInfo<ItemRecord>().GetValue(reader, 0, 2)).Should().Throw<InvalidOperationException>();
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

		private static Action Invoking(Action action) => action;

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
