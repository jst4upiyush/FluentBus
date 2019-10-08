using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using FluentBus.RabbitMq;

namespace FluentBus.RabbitMq
{
    public class RabbitMqConfiguration
    {
        public string Connection { get; set; }
        public int RetryCount { get; set; } = 5;
    }

    public static class RabbitMqMessageBusExtension
    {
        public static IMessageBusBuilder AddRabbitMqBus(
            this IServiceCollection services,
            Action<RabbitMqConfiguration> rabbitMqConfigFactory)
        {
            services.Configure(rabbitMqConfigFactory);
            services.TryAddTransient<IMessagePublisher, RabbitMqMessageBus>();
            services.TryAddSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>();

            services.TryAddSingleton<IHostedService, SubscriptionManagerService>();
            services.TryAddTransient(typeof(NotificationHandlerWrapperImpl<>));

            services.AddSubscriptionMediator();

            return new DefaultMessageBusBuilder(services);
        }
    }
}
