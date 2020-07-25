using System;
using System.Buffers;
using System.Text;
using A6k.Nats.Operations;
using A6k.Nats.Protocol;
using Xunit;

namespace A6k.Nats.Tests
{
    public class ProtocolTests
    {
        [Fact]
        public void BasicReadOp()
        {
            var op = "pub sub1 1\r\nx";
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(op));

            var reader = new NatsOperationReader();

            var consumed = buffer.Start;
            var examined = buffer.End;

            if (!reader.TryParseMessage(buffer, ref consumed, ref examined, out var msg))
                throw new InvalidOperationException("bad parse");

            Assert.Equal(NatsOperationId.PUB, msg.OpId);
            Assert.NotNull(msg.Op);
            var pub = Assert.IsType<PubOperation>(msg.Op);
            Assert.Equal("sub1", pub.Subject);
            Assert.Equal(1, pub.Data.Length);
            Assert.Equal((byte)'x', pub.Data.Span[0]);
        }


        [Fact]
        public void ParseInfo()
        {
            var o = "info {\"server_id\":\"svr1\"}\r\n";
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(o));

            var reader = new NatsOperationReader();

            var consumed = buffer.Start;
            var examined = buffer.End;

            if (!reader.TryParseMessage(buffer, ref consumed, ref examined, out var op))
                throw new InvalidOperationException("bad parse");

            Assert.Equal(NatsOperationId.INFO, op.OpId);
            Assert.NotNull(op.Op);
            Assert.IsType<ServerInfo>(op.Op);

            var info = (ServerInfo)op.Op;
            Assert.Equal("svr1", info.ServerId);
        }

        [Fact]
        public void ParseMsg()
        {
            var o = "msg s r 1\r\n";
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(o));

            var reader = new NatsOperationReader();

            var consumed = buffer.Start;
            var examined = buffer.End;

            if (!reader.TryParseMessage(buffer, ref consumed, ref examined, out var op))
                throw new InvalidOperationException("bad parse");

            Assert.Equal(NatsOperationId.MSG, op.OpId);
            Assert.NotNull(op.Op);
            Assert.IsType<MsgOperation>(op.Op);

            var msg = (MsgOperation)op.Op;
            Assert.Equal("s", msg.Subject);
            Assert.Equal("r", msg.ReplyTo);
            Assert.Equal(1, msg.NumBytes);
        }
    }
}
