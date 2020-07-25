namespace A6k.Nats.Operations
{
    public class UnSubOperation
    {
        public string Sid { get; set; }
        public int MaxMessages { get; set; }

        public override string ToString() => $"sid:{Sid} max_messages:{MaxMessages}";
    }
}
