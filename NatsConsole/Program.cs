using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace NatsConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var builder = new ConnectionBuilder()
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
        }
    }
}
