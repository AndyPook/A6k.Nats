using System;
using System.Buffers;
using Bedrock.Framework.Infrastructure;

namespace A6k.Nats.Operations
{
    public readonly struct PubOperation
    {
        public PubOperation(string subject, string replyTo, ReadOnlyMemory<byte> data)
        {
            Subject = subject;
            ReplyTo = replyTo;
            Data = data;
        }
        public PubOperation(string subject, string replyTo, ReadOnlySequence<byte> data)
        {
            Subject = subject;
            ReplyTo = replyTo;
            Data = data.ToMemory();
        }

        public string Subject { get; }
        public string ReplyTo { get; }

        public ReadOnlyMemory<byte> Data { get; }

        public override string ToString() => $"subject:{Subject} data:{Data.Length}";
    }
}
