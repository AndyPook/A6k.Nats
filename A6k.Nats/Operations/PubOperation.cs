using System;

namespace A6k.Nats.Operations
{
    public class PubOperation
    {
        public string Subject { get; set; }
        public string ReplyTo { get; set; }
        public byte[] Data { get; set; }

        public override string ToString() => $"subject:{Subject} data:{Data.Length}";
    }
}
