﻿using System;
using System.Threading.Tasks;
using A6k.Nats.Operations;

namespace A6k.Nats.Protocol
{
    public interface INatsSubscriptionManager
    {
        ValueTask InvokeAsync(MsgOperation msg);
        void Sub(string subject, string sid, IMessageSubscription handler);
        void UnSub(string sid);
    }
}
