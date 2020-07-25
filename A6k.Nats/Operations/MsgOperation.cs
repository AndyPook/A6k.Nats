namespace A6k.Nats.Operations
{
    public class MsgOperation
    {
        public string Subject { get; set; }
        public string Sid { get; set; }
        public string ReplyTo { get; set; }
        public long NumBytes { get; set; }
        public byte[] Data { get; set; }

        public override string ToString() => $"subject:{Subject} Sid:{Sid} bytes:{NumBytes}";
    }
}
