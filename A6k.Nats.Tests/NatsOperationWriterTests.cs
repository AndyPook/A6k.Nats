using A6k.Nats.Operations;
using A6k.Nats.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace A6k.Nats.Tests
{
    public class NatsOperationWriterTests
    {
        /// <summary>
        /// This test currently fails
        /// </summary>
        [Fact]
        public void WriteMessage_ConnectOperation_must_write_CONNECT_command_and_JSON_args()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var sut = new NatsOperationWriter();
            sut.WriteMessage(new NatsOperation(NatsOperationId.CONNECT, new ConnectOperation()), buffer);

            var writtenText = System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
            var jsonArgPos = writtenText.IndexOf(' ');
            var jsonArg = writtenText.AsSpan().Slice(jsonArgPos + 1).Trim();
            Assert.StartsWith("CONNECT", writtenText);
            Assert.EndsWith("\r\n", writtenText);
        }
    }
}
