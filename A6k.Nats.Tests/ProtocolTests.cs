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
            var op = "info {\"server_id\":\"abc\"}\r\n";
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(op));

            var reader = new NatsOperationReader();

            var consumed = buffer.Start;
            var examined = buffer.End;

            if (!reader.TryParseMessage(buffer, ref consumed, ref examined, out var msg))
                throw new InvalidOperationException("bad parse");

            Assert.Equal(NatsOperationId.INFO, msg.OpId);
            Assert.NotNull(msg.Op);
            var info = Assert.IsType<ServerInfo>(msg.Op);
            Assert.Equal("abc", info.ServerId);
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
            var o = "msg sub sid 1\r\n \r\n";
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
            Assert.Equal("sub", msg.Subject);
            Assert.Equal("sid", msg.Sid);
            Assert.Null(msg.ReplyTo);
            Assert.Equal(1, msg.NumBytes);
            Assert.Equal(1, msg.Data.Length);
            Assert.Equal((byte)' ', msg.Data.ToArray()[0]);
        }

        [Fact]
        public void ParseMsgWithReply()
        {
            var o = "msg sub sid r 1\r\n \r\n";
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
            Assert.Equal("sub", msg.Subject);
            Assert.Equal("sid", msg.Sid);
            Assert.Equal("r", msg.ReplyTo);
            Assert.Equal(1, msg.NumBytes);
            Assert.Equal(1, msg.Data.Length);
            Assert.Equal((byte)' ', msg.Data.ToArray()[0]);
        }
    }
}
