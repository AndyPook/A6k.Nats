using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using A6k.Nats.Operations;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;

namespace A6k.Nats
{
    public class NatsClientProtocol
    {
        public delegate ValueTask MsgHandler(string sid, ReadOnlySpan<byte> data);

        private readonly ConnectionContext connection;
        private readonly NatsOperationWriter opWriter;
        private ChannelWriter<NatsOperation> outboundWriter;

        public NatsClientProtocol(ConnectionContext connection)
        {
            this.connection = connection;
            opWriter = new NatsOperationWriter();
            StartInbound();
            StartOutbound();
        }

        public ServerInfo Info { get; private set; }

        public MsgHandler OnMsg { get; set; }

        public void Ping() => Send(new NatsOperation(NatsOperationId.PING));
        public void Pong() => Send(new NatsOperation(NatsOperationId.PONG));
        public void Pub(string subject, byte[] data)
            => Send(new NatsOperation(NatsOperationId.PUB, new PubOperation { Subject = subject, Data = data }));

        public void Sub(string subject, string sid)
            => Send(new NatsOperation(NatsOperationId.SUB, new SubOperation { Subject = subject, Sid = sid }));


        private ValueTask Send(NatsOperation operation) => outboundWriter.WriteAsync(operation);

        private void StartOutbound(CancellationToken cancellationToken = default)
        {
            // adding a bound here just to protect myself
            // I wouldn't expect this to grow too large
            // should add some metrics for monitoring
            var channel = Channel.CreateBounded<NatsOperation>(new BoundedChannelOptions(100) { SingleReader = true });
            outboundWriter = channel.Writer;
            var reader = channel.Reader;

            _ = ProcessOutbound(reader, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask ProcessOutbound(ChannelReader<NatsOperation> outboundReader, CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                await foreach (var op in outboundReader.ReadAllAsync(cancellationToken))
                {
                    opWriter.WriteMessage(op, connection.Transport.Output);

                    // add option for per-op flush
                    await connection.Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /* ignore cancellation */ }
        }


        private void StartInbound(CancellationToken cancellationToken = default)
            => ProcessInbound(cancellationToken).ConfigureAwait(false);

        private async Task ProcessInbound(CancellationToken cancellationToken)
        {
            await Task.Yield();
            var protocolReader = connection.CreateReader();
            var opReader = new NatsOperationReader();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await protocolReader.ReadAsync(opReader, cancellationToken).ConfigureAwait(false);
                    if (result.IsCompleted)
                        break;
                    protocolReader.Advance();

                    await HandleOperation(result.Message);
                }
                catch (Exception)
                {
                    if (!connection.ConnectionClosed.IsCancellationRequested)
                        throw;
                    break;
                }
            }

            Console.WriteLine("!!! exit inbound");
        }

        private ValueTask HandleOperation(NatsOperation op)
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
                    Console.WriteLine($"--- ERR: {Encoding.UTF8.GetString(op.Fields)}");
                    break;

                case NatsOperationId.INFO:
                    Console.WriteLine($"--- INFO: {Encoding.UTF8.GetString(op.Fields)}");
                    Info = op.Op as ServerInfo;
                    break;

                case NatsOperationId.MSG:
                    var msg = op.Op as MsgOperation;
                    Console.WriteLine($"--- MSG: {Encoding.UTF8.GetString(op.Fields)} sid:{msg.Sid} data:{Encoding.UTF8.GetString(msg.Data)}");
                    if (OnMsg != null)
                        return OnMsg.Invoke(msg.Sid, msg.Data);
                    break;

                default:
                    Console.WriteLine($"--- {op.OpId}: {Encoding.UTF8.GetString(op.Fields)}");
                    break;
            }

            return default;
        }
    }
}
