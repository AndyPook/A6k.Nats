using System;
using System.Text;

namespace A6k.Nats
{
    public enum NatsOperationId : long
    {
        INFO = 314845843200,
        CONNECT = 4850181421777769472,
        PUB = 1347764736,
        SUB = 1398096384,
        UNSUB = 93794893906432,
        MSG = 1297303296,
        PING = 344827250432,
        PONG = 344927913728,
        OK = 726616832,
        ERR = 194436551168
    }

    public readonly struct NatsOperation
    {
        //public static readonly long INFO = GetOpId("INFO");
        //public static readonly long CONNECT = GetOpId("CONNECT");
        //public static readonly long PUB = GetOpId("PUB");
        //public static readonly long SUB = GetOpId("SUB");
        //public static readonly long UNSUB = GetOpId("UNSUB");
        //public static readonly long MSG = GetOpId("MSG");
        //public static readonly long PING = GetOpId("PING");
        //public static readonly long PONG = GetOpId("PONG");
        //public static readonly long OK = GetOpId("+OK");
        //public static readonly long ERR = GetOpId("-ERR");

        public static NatsOperationId GetOpId(ReadOnlySpan<byte> opName)
        {
            long id = 0;
            for (int i = 0; i < opName.Length; i++)
            {
                if (opName[i] > 96)
                    id += opName[i] - 0x20;
                else
                    id += opName[i];
                id <<= 8;
            }
            return (NatsOperationId)id;
        }

        public static NatsOperationId GetOpId(string opName) => GetOpId(Encoding.ASCII.GetBytes(opName));

        private readonly ReadOnlyMemory<byte> fields;

        public NatsOperation(NatsOperationId opId, ReadOnlyMemory<byte> fields, object op)
        {
            OpId = opId;
            this.fields = fields;
            Op = op;
        }

        public NatsOperationId OpId { get; }
        public ReadOnlySpan<byte> Fields => fields.Span;

        public object Op { get; }

        public override string ToString() => $"{OpId} - {Op}";
    }
}
