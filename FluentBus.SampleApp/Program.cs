using System;
using System.Text;
using System.Threading;
using FluentBus.Core;

namespace FluentBus.SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var rmqOptions = new RabbitMQOptions();
            RabbitMQConsumerClient client = new RabbitMQConsumerClient(
                "fluentbus.samplequeue",
                new ConnectionChannelPool("ex.fluentbus.sample", rmqOptions),
                rmqOptions
                );
            client.OnMessageReceived += (sender, transportMessage) =>
            {
                Console.WriteLine(Encoding.UTF8.GetString(transportMessage.Body));
                client.Commit(sender);
            };

            client.Listening(TimeSpan.FromSeconds(1), CancellationToken.None);
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}
