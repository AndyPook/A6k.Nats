using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using A6k.Nats.Operations;
using A6k.Nats.Protocol;
using Bedrock.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A6k.Nats
{
    public class NatsClient : INatsOperationHandler
    {
        private NatsClientProtocol nats;
        private INatsSubscriptionManager subscriptions = new NatsSubscriptionManager();
        private long ssid = 0;
        private ILogger logger;

        public ServerInfo Info { get; private set; }

        public async ValueTask StartAsync(EndPoint endpoint, IServiceProvider serviceProvider)
        {
            if (serviceProvider is null)
                throw new ArgumentNullException(nameof(serviceProvider));

            logger = serviceProvider.GetRequiredService<ILogger<NatsClient>>();
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

        public void Publish(string subject, string replyto, byte[] data)
            => nats.Send(NatsOperationId.PUB, new PubOperation(subject, replyto, data));

        public ISubscription Subscribe(string subject, string queueGroup, IMessageSubscription handler)
        {
            var sid = GetNextSid();
            subscriptions.Sub(subject, sid, handler);
            nats.Send(NatsOperationId.SUB, new SubOperation(subject, queueGroup, sid));
            return new Subscription(this, subject, queueGroup, sid);
        }
        private string GetNextSid() => Interlocked.Increment(ref ssid).ToString(CultureInfo.InvariantCulture);

        public void UnSub(string sid, int? maxMessages = default)
        {
            subscriptions.UnSub(sid);
            nats.Send(NatsOperationId.UNSUB, new UnSubOperation(sid, maxMessages));
        }

        ValueTask INatsOperationHandler.HandleOperation(NatsOperation op)
        {
            switch (op.OpId)
            {
                case NatsOperationId.PING:
                    logger.LogTrace("--- ping");
                    Pong();
                    break;
                case NatsOperationId.PONG:
                    logger.LogTrace("--- pong");
                    break;
                case NatsOperationId.OK:
                    logger.LogTrace("--- OK");
                    break;
                case NatsOperationId.ERR:
                    var err = (ErrOperation)op.Op;
                    logger.LogTrace($"--- ERR: {err}");
                    break;

                case NatsOperationId.INFO:
                    Info = (ServerInfo)op.Op;
                    logger.LogTrace($"--- INFO: {Info}");
                    break;

                case NatsOperationId.MSG:
                    var msg = (MsgOperation)op.Op;
                    logger.LogTrace($"--- MSG: {op.Op}");
                    return subscriptions.InvokeAsync(msg);

                default:
                    logger.LogError("--- UNSUPPORTED - {OpId}", op.OpId);
                    break;
            }

            return default;
        }

        void INatsOperationHandler.ConnectionClosed()
        {
            Console.WriteLine("Connection Closed");
        }
    }

    public interface ISubscription : IDisposable
    {
        string Subject { get; }
        string Queue { get; }
        string Sid { get; }
    }
    public class Subscription : ISubscription
    {
        private readonly NatsClient nats;

        public Subscription(NatsClient nats, string subject, string queue, string sid)
        {
            this.nats = nats;
            Subject = subject;
            Queue = queue;
            Sid = sid;
        }

        public string Subject { get; }
        public string Queue { get; }
        public string Sid { get; }

        public void Dispose() => nats.UnSub(Sid);
    }
}
