using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using A6k.Nats;
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

            nats.Sub("test2", "1", msg =>
            {
                var text = Encoding.UTF8.GetString(msg.Data.Span);
                Console.WriteLine($"OnMsg: subject:{msg.Subject} sid:{msg.Sid} replyto:{msg.ReplyTo} text:{text}");
            });

            //nats.OnMsg = (sid, replyto, data) =>
            //{
            //    var text = Encoding.UTF8.GetString(data);
            //    Console.WriteLine($"OnMsg: sid:{sid} replyto:{replyto} text:{text}");
            //    return default;
            //};

            //nats.Sub("test2", "1");

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
