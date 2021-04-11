using System;
using System.Threading.Tasks;
using A6k.Nats.Operations;

namespace A6k.Nats
{
    public static class NatsClientExtensions
    {
        public static void Publish(this NatsClient nats, string subject, byte[] data)
            => nats.Publish(subject, null, data);

        public static ISubscription Subscribe(this NatsClient nats, string subject, IMessageSubscription handler)
            => nats.Subscribe(subject, default, handler);

        public static ISubscription Subscribe(this NatsClient nats, string subject, Action<MsgOperation> handler)
            => nats.Subscribe(subject, new SyncMessageSubscription(handler));

        public static ISubscription Subscribe(this NatsClient nats, string subject, Func<MsgOperation, ValueTask> handler)
            => nats.Subscribe(subject, new ValueTaskMessageSubscription(handler));

        public static ISubscription Subscribe(this NatsClient nats, string subject, Func<MsgOperation, Task> handler)
            => nats.Subscribe(subject, new TaskMessageSubscription(handler));

        public static ISubscription Subscribe(this NatsClient nats, string subject, string queueGroup, Action<MsgOperation> handler)
            => nats.Subscribe(subject, queueGroup, new SyncMessageSubscription(handler));

        public static ISubscription Subscribe(this NatsClient nats, string subject, string queueGroup, Func<MsgOperation, ValueTask> handler)
            => nats.Subscribe(subject, queueGroup, new ValueTaskMessageSubscription(handler));

        public static ISubscription Subscribe(this NatsClient nats, string subject, string queueGroup, Func<MsgOperation, Task> handler)
            => nats.Subscribe(subject, queueGroup, new TaskMessageSubscription(handler));
    }
}
