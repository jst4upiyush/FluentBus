using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentBus.RabbitMq
{
    public class PublisherBuilder
    {
        public PublisherBuilder(IMessageBusBuilder messageBus) => MessageBus = messageBus;

        public IMessageBusBuilder MessageBus { get; }
    }

    public class RabbitMqPublisherOptions
    {
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Fanout;
        public byte DeliveryMode { get; set; } = 2;
        public Func<object, string> Serializer { get; set; } = JsonConvert.SerializeObject;
        internal List<Action<IBasicProperties>> BasicProperties { get; set; } = new List<Action<IBasicProperties>>();
    }

    public static class PublisherOptionsExtensions
    {
        public static PublisherBuilder Configure<TMessage>(
            this PublisherBuilder builder,
            Action<RabbitMqPublisherOptions> configureOptions)
                where TMessage : class
        {
            builder.MessageBus.Services.Configure(typeof(TMessage).Name, configureOptions);
            return builder;
        }

        public static PublisherBuilder Defaults(
            this PublisherBuilder builder,
            Action<RabbitMqPublisherOptions> configureOptions)
        {
            builder.MessageBus.Services.ConfigureAll(configureOptions);
            return builder;
        }
    }

}
