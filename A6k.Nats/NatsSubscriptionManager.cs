using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using A6k.Nats.Operations;
using A6k.Nats.Protocol;

namespace A6k.Nats
{
    public class NatsSubscriptionManager : INatSubscriptionManager
    {
        private ConcurrentDictionary<(string Subject, string Sid), IMessageSubscription> subscriptions = new ConcurrentDictionary<(string Subject, string Sid), IMessageSubscription>();

        public ValueTask InvokeAsync(MsgOperation msg)
        {
            if (subscriptions.TryGetValue((msg.Subject, msg.Sid), out var handler))
                return handler.HandleAsync(msg);

            //var text = Encoding.UTF8.GetString(data);
            //Console.WriteLine($"OnMsg: sid:{sid} replyto:{replyto} text:{text}");
            return default;
        }

        public void Sub(string subject, string sid, IMessageSubscription handler)
        {
            subscriptions[(subject, sid)] = handler;
        }

        public void UnSub(string subject, string sid)
        {
            subscriptions.TryRemove((subject, sid), out _);
        }
    }
}
