namespace A6k.Nats.Operations
{
    public class SubOperation
    {
        public string Subject { get; set; }
        public string QueueGroup { get; set; }
        public string Sid { get; set; }

        public override string ToString() => $"subject:{Subject} queue:{QueueGroup} sid:{Sid}";
    }
}
