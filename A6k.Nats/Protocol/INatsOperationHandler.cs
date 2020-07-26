using System.Threading.Tasks;

namespace A6k.Nats.Protocol
{
    public interface INatsOperationHandler
    {
        ValueTask HandleOperation(NatsOperation op);
        void ConnectionClosed();
    }
}
