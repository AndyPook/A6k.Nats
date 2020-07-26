namespace A6k.Nats.Operations
{
    public readonly struct SubOperation
    {
        public SubOperation(string subject, string sid)
        {
            Subject = subject;
            QueueGroup = default;
            Sid = sid;
        }
        public SubOperation(string subject, string queueGroup, string sid)
        {
            Subject = subject;
            QueueGroup = queueGroup;
            Sid = sid;
        }

        public string Subject { get; }
        public string QueueGroup { get; }
        public string Sid { get; }

        public override string ToString() => $"subject:{Subject} queue:{QueueGroup} sid:{Sid}";
    }
}
