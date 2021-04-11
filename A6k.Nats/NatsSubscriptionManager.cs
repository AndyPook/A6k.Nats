using System.Collections.Concurrent;
using System.Threading.Tasks;
using A6k.Nats.Operations;
using A6k.Nats.Protocol;

namespace A6k.Nats
{
    public class NatsSubscriptionManager : INatsSubscriptionManager
    {
        private ConcurrentDictionary<string, IMessageSubscription> subscriptions = new ConcurrentDictionary<string, IMessageSubscription>();

        public ValueTask InvokeAsync(MsgOperation msg)
        {
            if (subscriptions.TryGetValue(msg.Sid, out var handler))
                return handler.HandleAsync(msg);

            return default;
        }

        public void Sub(string subject, string sid, IMessageSubscription handler)
        {
            subscriptions[sid] = handler;
        }

        public void UnSub(string sid)
        {
            subscriptions.TryRemove(sid, out _);
        }
    }
}
