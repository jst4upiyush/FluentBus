using FluentBus.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace FluentBus.RabbitMq
{
    public class SubscriptionBuilder
    {
        public SubscriptionBuilder(IMessageBusBuilder messageBus) => MessageBus = messageBus;

        public IMessageBusBuilder MessageBus { get; }
    }

    public class RabbitMqSubscriptionOptions
    {
        public string Queue { get; set; }

        public string Exchange { get; set; }
        public string ExchangeType { get; set; }

        public string RoutingKey { get; set; }

        internal Type MessageType { get; set; }

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public Func<string, INotificationMessage> DeserializationFactory { get; set; }

        public Func<IRabbitMqMessage, INotificationMessage> MessageFactory { get; set; }
    }

    public class SubscriptionManagerConfig
    {
        public ConcurrentDictionary<string, Type> RegisteredSubscriptions { get; } = new ConcurrentDictionary<string, Type>();
    }

    public static class MessageBusSubscriptionExtentions
    {
        public static SubscriptionBuilder Add<TMessage>(
            this SubscriptionBuilder subscriptionOptions,
            Action<RabbitMqSubscriptionOptions> configureOptions)
                where TMessage : INotificationMessage
        {
            string name = typeof(TMessage).Name;

            subscriptionOptions.MessageBus.Services
                .ConfigureSubscriptionDefaults<TMessage>()
                .Configure(name, configureOptions)
                .Configure<RabbitMqSubscriptionOptions>(name, config => config.MessageType = typeof(TMessage))
                .Configure<SubscriptionManagerConfig>(config => config.RegisteredSubscriptions.TryAdd(name, typeof(TMessage)));

            return subscriptionOptions;
        }

        private static IServiceCollection ConfigureSubscriptionDefaults<TMessage>(
            this IServiceCollection services)
                where TMessage : INotificationMessage
        {
            string name = typeof(TMessage).Name;
            return services.Configure<RabbitMqSubscriptionOptions>(name, config =>
            {
                config.MessageType = typeof(TMessage);
                config.Queue = name;
                config.DeserializationFactory = msg => JsonConvert.DeserializeObject<TMessage>(msg);
                config.MessageFactory = msg => msg.Message;
            });
        }
    }
}
