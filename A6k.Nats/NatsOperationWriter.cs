using System;
using System.Buffers;
using A6k.Nats.Operations;
using Bedrock.Framework.Protocols;

namespace A6k.Nats
{
    public class NatsOperationWriter : IMessageWriter<NatsOperation>
    {
        private const byte CR = (byte)'\r';
        private const byte LF = (byte)'\n';

        private static ReadOnlySpan<byte> CRLF => new byte[] { CR, LF };

        public void WriteMessage(NatsOperation operation, IBufferWriter<byte> output)
        {
            var writer = new NatsWriter(output);
            switch (operation.OpId)
            {
                case NatsOperationId.PING:
                    writer.WriteString("PING\r\n");
                    break;
                case NatsOperationId.PONG:
                    writer.WriteString("PONG\r\n");
                    break;

                case NatsOperationId.PUB:
                    WritePub(ref writer, operation.Op as PubOperation);
                    break;
                case NatsOperationId.SUB:
                    WriteSub(ref writer, operation.Op as SubOperation);
                    break;
            }
            writer.Commit();
        }

        private static void WritePub(ref NatsWriter writer, PubOperation op)
        {
            writer.WriteString($"PUB {op.Subject} ");
            if (!string.IsNullOrEmpty(op.ReplyTo))
            {
                writer.WriteString(op.ReplyTo);
                writer.WriteString(" ");
            }
            writer.WriteInt(op.Data.Length);
            writer.Write(CRLF);
            writer.Write(op.Data);
            writer.Write(CRLF);
        }

        private static void WriteSub(ref NatsWriter writer, SubOperation op)
        {
            writer.WriteString($"SUB {op.Subject}");
            if (!string.IsNullOrEmpty(op.QueueGroup))
            {
                writer.WriteString(" ");
                writer.WriteString(op.QueueGroup);
            }
            writer.WriteString(" ");
            writer.WriteString(op.Sid);
            writer.Write(CRLF);
        }
    }
}
