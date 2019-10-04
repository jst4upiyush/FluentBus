using Microsoft.Extensions.DependencyInjection;

namespace FluentBus.RabbitMq
{
    public interface IMessageBusBuilder
    {
        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        IServiceCollection Services { get; }

        SubscriptionBuilder Subscriptions { get; }

        PublisherBuilder Publishers { get; }
    }

    internal class DefaultMessageBusBuilder : IMessageBusBuilder
    {
        public DefaultMessageBusBuilder(IServiceCollection services)
        {
            Services = services;
            Subscriptions = new SubscriptionBuilder(this);
            Publishers = new PublisherBuilder(this);
        }

        public IServiceCollection Services { get; }

        public SubscriptionBuilder Subscriptions { get; }

        public PublisherBuilder Publishers { get; }
    }
}
