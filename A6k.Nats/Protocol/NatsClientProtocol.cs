using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;

namespace A6k.Nats.Protocol
{
    public class NatsClientProtocol
    {
        public delegate ValueTask MsgHandler(string sid, string replyto, ReadOnlySpan<byte> data);

        private readonly ConnectionContext connection;
        private readonly INatsOperationHandler operationHandler;
        private ChannelWriter<NatsOperation> outboundChannel;

        public NatsClientProtocol(ConnectionContext connection, INatsOperationHandler operationHandler)
        {
            this.connection = connection;
            this.operationHandler = operationHandler;
            StartInbound(connection.ConnectionClosed);
            StartOutbound(connection.ConnectionClosed);
        }

        public ValueTask Send(NatsOperationId opId, object op = default) => outboundChannel.WriteAsync(new NatsOperation(opId, op));

        private void StartOutbound(CancellationToken cancellationToken = default)
        {
            // adding a bound here just to protect myself
            // I wouldn't expect this to grow too large
            // should add some metrics for monitoring
            var channel = Channel.CreateBounded<NatsOperation>(new BoundedChannelOptions(100) { SingleReader = true });
            outboundChannel = channel.Writer;
            var reader = channel.Reader;

            _ = ProcessOutbound(reader, cancellationToken).ConfigureAwait(false);
        }

        private async Task ProcessOutbound(ChannelReader<NatsOperation> outboundReader, CancellationToken cancellationToken)
        {
            await Task.Yield();
            var opWriter = new NatsOperationWriter();

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
            catch (IOException iox)
            {
                Console.WriteLine("outbound connection failed: " + iox.Message);
                operationHandler.ConnectionClosed();
            }
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
                    {
                        Console.WriteLine("inbound connection closed");
                        break;
                    }
                    protocolReader.Advance();

                    await operationHandler.HandleOperation(result.Message);
                }
                catch (OperationCanceledException) { /* ignore cancellation */ }
                catch (Exception)
                {
                    if (!connection.ConnectionClosed.IsCancellationRequested)
                        throw;
                    break;
                }
            }

            Console.WriteLine("!!! exit inbound");
            operationHandler.ConnectionClosed();
        }
    }
}
