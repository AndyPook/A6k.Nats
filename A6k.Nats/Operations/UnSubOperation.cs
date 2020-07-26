namespace A6k.Nats.Operations
{
    public readonly struct UnSubOperation
    {
        public UnSubOperation(string sid, int? maxMessages = default)
        {
            Sid = sid;
            MaxMessages = maxMessages;
        }

        public string Sid { get; }
        public int? MaxMessages { get; }

        public override string ToString() => $"sid:{Sid} max_messages:{MaxMessages}";
    }
}
