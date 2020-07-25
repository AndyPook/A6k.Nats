using System.Threading.Tasks;
using A6k.Nats.Operations;

namespace A6k.Nats
{
    public interface IMessageSubscription
    {
        ValueTask HandleAsync(MsgOperation msg);
    }
}
