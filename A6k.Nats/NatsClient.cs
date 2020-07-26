using System;
using System.Net;
using System.Threading.Tasks;
using A6k.Nats.Operations;
using A6k.Nats.Protocol;
using Bedrock.Framework;

namespace A6k.Nats
{
    public static class NatsCLientExtensions
    {
        public static void Sub(this NatsClient nats, string subject, string sid, Action<MsgOperation> handler)
        {
            nats.Sub(subject, sid, new DelegateMessageSubscription(handler));
        }

        public static void Sub(this NatsClient nats, string subject, string sid, Func<MsgOperation, ValueTask> handler)
        {
            nats.Sub(subject, sid, new DelegateMessageSubscription(handler));
        }
    }

    public class NatsClient : INatsOperationHandler
    {
        private NatsClientProtocol nats;
        private INatsSubscriptionManager subscriptions = new NatsSubscriptionManager();

        public ServerInfo Info { get; private set; }

        public async Task ConnectAsync(EndPoint endpoint, IServiceProvider serviceProvider)
        {
            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var client = new ClientBuilder(serviceProvider)
                .UseSockets()
                //.UseConnectionLogging()
                .Build();

            var conn = await client.ConnectAsync(endpoint);
            nats = new NatsClientProtocol(conn, this);
        }


        public void Ping() => nats.Send(NatsOperationId.PING);
        public void Pong() => nats.Send(NatsOperationId.PONG);
        public void Connect(ConnectOperation connect) => nats.Send(NatsOperationId.CONNECT, connect);

        public void Pub(string subject, byte[] data)
            => nats.Send(NatsOperationId.PUB, new PubOperation(subject, null, data));
        public void Pub(string subject, string replyto, byte[] data)
            => nats.Send(NatsOperationId.PUB, new PubOperation(subject, replyto, data));

        public void Sub(string subject, string sid, IMessageSubscription handler)
        {
            subscriptions.Sub(subject, sid, handler);
            nats.Send(NatsOperationId.SUB, new SubOperation(subject, sid));
        }
        public void UnSub(string sid, int? maxMessages = default)
        {
            //subscriptions.UnSub(sid);
            nats.Send(NatsOperationId.UNSUB, new UnSubOperation(sid, maxMessages));
        }

        ValueTask INatsOperationHandler.HandleOperation(NatsOperation op)
        {
            switch (op.OpId)
            {
                case NatsOperationId.PING:
                    Console.WriteLine("--- ping");
                    Pong();
                    break;
                case NatsOperationId.PONG:
                    Console.WriteLine("--- pong");
                    break;
                case NatsOperationId.OK:
                    Console.WriteLine("--- OK");
                    break;
                case NatsOperationId.ERR:
                    var err = (ErrOperation)op.Op;
                    Console.WriteLine($"--- ERR: {err.Message}");
                    break;

                case NatsOperationId.INFO:
                    Console.WriteLine($"--- INFO: {op.Op}");
                    Info = op.Op as ServerInfo;
                    break;

                case NatsOperationId.MSG:
                    var msg = (MsgOperation)op.Op;
                    Console.WriteLine($"--- MSG: {op.Op}");
                    return subscriptions.InvokeAsync(msg);

                default:
                    Console.WriteLine($"--- UNSUPPORTED - {op.OpId}");
                    break;
            }

            return default;
        }

    }
}
