using System;

namespace A6k.Nats.Operations
{
    public readonly struct MsgOperation
    {
        public MsgOperation(string subject, string sid, string replyTo, int numBytes, ReadOnlySpan<byte> data)
        {
            Subject = subject;
            Sid = sid;
            ReplyTo = replyTo;
            NumBytes = numBytes;
            Data = data.ToArray();
        }

        public string Subject { get; }
        public string Sid { get; }
        public string ReplyTo { get; }
        public int NumBytes { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public override string ToString() => $"subject:{Subject} Sid:{Sid} data:{NumBytes}";
    }
}
