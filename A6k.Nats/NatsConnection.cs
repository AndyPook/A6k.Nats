using System;
using System.Buffers;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Connections;

using System.Net;
using System.Collections.Generic;
using System.Linq;
using Bedrock.Framework.Protocols;

namespace A6k.Nats
{
    //public partial class NatsConnection //: INatsConnection, IAsyncDisposable
    //{
    //    private readonly ConnectionContext connection;
    //    private readonly IPEndPoint endpoint;
    //    private CancellationTokenSource cancellation = new CancellationTokenSource();

    //    private ChannelWriter<NatsOperation> outboundWriter;
    //    private ChannelWriter<NatsOperation> inflightWriter;

    //    public NatsConnection(ConnectionContext connection, IPEndPoint endpoint, string clientId)
    //    {
    //        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    //        this.endpoint = endpoint;
    //    }

    //    public async ValueTask Send(string op)
    //    {
    //        await EnsureConnection();

    //        outboundWriter.TryWrite(default);
    //    }

    //    private async ValueTask EnsureConnection()
    //    {
    //        if (connection is null)
    //        {
    //            //connection = await connectionBuilder.Build();

    //            StartOutbound(cancellation.Token);
    //            StartInbound(cancellation.Token);

    //            Console.WriteLine("connected: " + endpoint);
    //        }
    //    }


    //    private void StartOutbound(CancellationToken cancellationToken = default)
    //    {
    //        // adding a bound here just to protect myself
    //        // I wouldn't expect this to grow too large
    //        // should add some metrics for monitoring
    //        var channel = Channel.CreateBounded<NatsOperation>(new BoundedChannelOptions(100) { SingleReader = true });
    //        outboundWriter = channel.Writer;
    //        var reader = channel.Reader;

    //        _ = ProcessOutbound(reader, cancellationToken).ConfigureAwait(false);
    //    }

    //    private async ValueTask ProcessOutbound(ChannelReader<NatsOperation> outboundReader, CancellationToken cancellationToken)
    //    {
    //        await Task.Yield();

    //        try
    //        {
    //            await foreach (var op in outboundReader.ReadAllAsync(cancellationToken))
    //                await SendRequest(op);
    //        }
    //        catch (OperationCanceledException) { /* ignore cancellation */ }

    //        async ValueTask SendRequest(NatsOperation op)
    //        {
    //            await inflightWriter.WriteAsync(op, cancellationToken);
    //            using var buffer = new MemoryBufferWriter();

    //            // write v1 Header
    //            //buffer.WriteShort(op.ApiKey);
    //            //buffer.WriteShort(op.Version);
    //            //buffer.WriteInt(op.CorrelationId);
    //            //buffer.WriteString(ClientId);

    //            op.WriteMessage(buffer);

    //            buffer.CopyTo(connection.Transport.Output);

    //            await connection.Transport.Output.FlushAsync().ConfigureAwait(false);
    //        }
    //    }

    //    private void StartInbound(CancellationToken cancellationToken = default)
    //    {
    //        // adding a bound here just to protect myself
    //        // I wouldn't expect this to grow too large
    //        // should add some metrics for monitoring
    //        var channel = Channel.CreateBounded<Op>(new BoundedChannelOptions(100) { SingleWriter = true, SingleReader = true });
    //        inflightWriter = channel.Writer;
    //        var reader = channel.Reader;

    //        _ = ProcessInbound(reader, cancellationToken).ConfigureAwait(false);
    //    }

    //    private async Task ProcessInbound(ChannelReader<Op> inflightReader, CancellationToken cancellationToken)
    //    {
    //        await Task.Yield();
    //        var natsReader = new NatsOperationReader();
    //        var protocolReader = connection.CreateReader();

    //        while (!cancellationToken.IsCancellationRequested)
    //        {
    //            try
    //            {
    //                var result = await protocolReader.ReadAsync(natsReader, cancellationToken);
    //                if (result.IsCompleted)
    //                    break;

    //                var op = result.Message;
    //                protocolReader.Advance();

    //                Console.WriteLine($"inbound: {op}");
    //            }
    //            catch (Exception)
    //            {
    //                if (!connection.ConnectionClosed.IsCancellationRequested)
    //                    throw;
    //                break;
    //            }
    //        }
    //    }

    //    public async ValueTask DisposeAsync()
    //    {
    //        cancellation.Cancel();
    //        await connection.DisposeAsync();
    //    }
    //}
}
