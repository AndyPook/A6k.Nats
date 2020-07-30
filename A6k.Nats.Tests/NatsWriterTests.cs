using System;
using System.Buffers;
using System.Text;
using A6k.Nats.Operations;
using A6k.Nats.Protocol;
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
            var sut = new NatsWriter(buffer);
            sut.WriteInt(number);
            sut.Commit();

            var writtenString = Encoding.UTF8.GetString(buffer.WrittenSpan);
            Assert.Equal(number.ToString(), writtenString);
        }
    }
}
