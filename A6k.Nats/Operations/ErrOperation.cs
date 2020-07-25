namespace A6k.Nats.Operations
{
    public readonly struct ErrOperation
    {
        public ErrOperation(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
