using System;
using System.Threading.Tasks;
using A6k.Nats.Operations;

namespace A6k.Nats
{
    public class DelegateMessageSubscription : IMessageSubscription
    {
        private readonly Func<MsgOperation, ValueTask> handler;

        public DelegateMessageSubscription(Func<MsgOperation, ValueTask> handler)
        {
            this.handler = handler;
        }
        public DelegateMessageSubscription(Action<MsgOperation> handler)
        {
            this.handler = m => { handler(m); return default; };
        }

        public ValueTask HandleAsync(MsgOperation msg) => handler(msg);
    }
    public class SyncMessageSubscription : IMessageSubscription
    {
        private readonly Func<MsgOperation, ValueTask> handler;

        public SyncMessageSubscription(Action<MsgOperation> handler)
        {
            this.handler = m => { handler(m); return default; };
        }

        public ValueTask HandleAsync(MsgOperation msg) => handler(msg);
    }
    public class ValueTaskMessageSubscription : IMessageSubscription
    {
        private readonly Func<MsgOperation, ValueTask> handler;

        public ValueTaskMessageSubscription(Func<MsgOperation, ValueTask> handler)
        {
            this.handler = handler;
        }

        public ValueTask HandleAsync(MsgOperation msg) => handler(msg);
    }
    public class TaskMessageSubscription : IMessageSubscription
    {
        private readonly Func<MsgOperation, Task> handler;

        public TaskMessageSubscription(Func<MsgOperation, Task> handler)
        {
            this.handler = handler;
        }

        public async ValueTask HandleAsync(MsgOperation msg) => await handler(msg);
    }
}
