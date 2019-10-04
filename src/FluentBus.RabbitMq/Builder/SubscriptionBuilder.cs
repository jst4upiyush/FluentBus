using FluentBus.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
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
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public string Exchange { get; set; }
        public string ExchangeType { get; set; }

        public string RoutingKey { get; set; }

        internal Type MessageType { get; set; }
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

            subscriptionOptions.MessageBus.Services.Configure(name, configureOptions);
            subscriptionOptions.MessageBus.Services.Configure<RabbitMqSubscriptionOptions>(name, config => config.MessageType = typeof(TMessage));
            subscriptionOptions.MessageBus.Services.Configure<SubscriptionManagerConfig>(config => config.RegisteredSubscriptions.TryAdd(name, typeof(TMessage)));

            return subscriptionOptions;
        }
    }
}
