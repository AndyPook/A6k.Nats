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

            var nats = new NatsClient();
            await nats.StartAsync(new IPEndPoint(IPAddress.Loopback, 4222), sp);

            nats.Connect(new ConnectOperation { Verbose = false });

            nats.Sub("test", msg =>
            {
                var text = Encoding.UTF8.GetString(msg.Data.Span);
                Console.WriteLine($"OnMsg: subject:{msg.Subject} sid:{msg.Sid} replyto:{msg.ReplyTo} text:{text}");
            });

            while (!cts.Token.IsCancellationRequested)
            {
                Console.WriteLine("pub...");
                nats.Pub("test", Encoding.UTF8.GetBytes("hello"));
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
