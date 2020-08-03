using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using A6k.Nats;
using A6k.Nats.Operations;
using Bedrock.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NatsConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // hit ctrl-C to close/exit

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                Console.WriteLine("cancelled...");
                cts.Cancel();
                e.Cancel = true;
            };

            var sp = ConfigureServices();

            var client = new ClientBuilder(sp)
                .UseSockets()
                //.UseConnectionLogging()
                .Build();

            var conn = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 4222));
            var nats = new NatsClient();
            await nats.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 4222), sp);

            nats.Connect(new ConnectOperation { Verbose = false });

            // test this with "pub test2 2\r\nhi" from telnet
            nats.Sub("test2", "1", msg =>
            {
                var text = Encoding.UTF8.GetString(msg.Data.Span);
                Console.WriteLine($"OnMsg: subject:{msg.Subject} sid:{msg.Sid} replyto:{msg.ReplyTo} text:{text}");
            });

            // test this with "sub test1 1" from telnet
            while (!cts.Token.IsCancellationRequested)
            {
                Console.WriteLine("pub...");
                nats.Pub("test1", Encoding.UTF8.GetBytes("hello"));
                await Task.Delay(2000);
            }

            Console.WriteLine("done...");
            Console.ReadLine();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

            return services.BuildServiceProvider();
        }
    }
}
