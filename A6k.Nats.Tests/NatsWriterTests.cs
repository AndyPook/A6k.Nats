using System;
using System.Buffers;
using System.Text;
using A6k.Nats.Operations;
using A6k.Nats.Protocol;
using Bedrock.Framework.Infrastructure;
using Xunit;

namespace A6k.Nats.Tests
{
    public class NatsWriterTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(9999)]
        [InlineData(-9999)]
        [InlineData(int.MinValue)]
        public void WriteInt_must_write_integer_to_buffer_as_a_string(int number)
        {
            var buffer = new ArrayBufferWriter<byte>(initialCapacity: 64);
            var writer = new BufferWriter<IBufferWriter<byte>>(buffer);
            writer.WriteInt(number);
            writer.Commit();

            var writtenString = Encoding.UTF8.GetString(buffer.WrittenSpan);
            Assert.Equal(number.ToString(), writtenString);
        }

        [Fact]
        public void WriteJson_must_serialize_large_object_as_utf8_json_into_buffer()
        {
            var objectToSerialize = new SerializableObject { SomeProp = 123, AnotherProp = new string('A', 1024) };
            var buffer = new ArrayBufferWriter<byte>(initialCapacity: 128);
            var writer = new BufferWriter<IBufferWriter<byte>>(buffer);
            writer.WriteJson(objectToSerialize);
            writer.Commit();

            var writtenString = Encoding.UTF8.GetString(buffer.WrittenSpan);
            var deserializedObject = System.Text.Json.JsonSerializer.Deserialize<SerializableObject>(writtenString);
            Assert.Equal(objectToSerialize.SomeProp, deserializedObject.SomeProp);
            Assert.Equal(objectToSerialize.AnotherProp, deserializedObject.AnotherProp);
        }

        [Fact]
        public void WriteJson_followed_by_other_writes_must_serialize_object_as_utf8_json_into_buffer()
        {
            var objectToSerialize = new SerializableObject { SomeProp = 123, AnotherProp = "foobar" };
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new BufferWriter<IBufferWriter<byte>>(buffer);
            writer.WriteJson(objectToSerialize);
            writer.Write(new byte[] { 65, 66, 67, 68 }); // ABCD as ASCII
            writer.Commit();

            var writtenString = Encoding.UTF8.GetString(buffer.WrittenSpan);
            Assert.EndsWith("ABCD", writtenString);
        }

        [Fact]
        public void WriteJson_preceded_by_other_writes_must_serialize_object_as_utf8_json_into_buffer()
        {
            var objectToSerialize = new SerializableObject { SomeProp = 123, AnotherProp = "foobar" };
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new BufferWriter<IBufferWriter<byte>>(buffer);
            writer.Write(new byte[] { 65, 66, 67, 68 }); // ABCD as ASCII
            writer.WriteJson(objectToSerialize);
            writer.Write(new byte[] { 48, 49, 50, 51 }); // 0123 as ASCII
            writer.Commit();

            var writtenString = Encoding.UTF8.GetString(buffer.WrittenSpan);
            Assert.StartsWith("ABCD", writtenString);
            Assert.EndsWith("0123", writtenString);
        }

        public class SerializableObject
        {
            public int SomeProp { get; set; }
            public string AnotherProp { get; set; }
        }
    }
}
