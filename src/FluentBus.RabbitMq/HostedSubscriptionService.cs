using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using System.Linq;
using System;

namespace FluentBus.RabbitMq
{
    internal class SubscriptionManagerService : IHostedService
    {
        private readonly IOptions<SubscriptionManagerConfig> _subscriptionConfig;
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IOptionsSnapshot<RabbitMqSubscriptionOptions> _subscriptionOptions;

        private List<ISubscription> _subscriptions = new List<ISubscription>();

        public SubscriptionManagerService(
            IServiceProvider services,
            IOptions<SubscriptionManagerConfig> subscriptionConfig,
            IRabbitMQPersistentConnection persistentConnection,
            IOptionsSnapshot<RabbitMqSubscriptionOptions> subscriptionOptions)
        {
            _subscriptionConfig = subscriptionConfig;
            _persistentConnection = persistentConnection;
            _subscriptionOptions = subscriptionOptions;

            _subscriptions = _subscriptionConfig.Value.RegisteredSubscriptions
                .Select(s => new RabbitMqSubscription(services, _persistentConnection, _subscriptionOptions, s.Key))
                .Cast<ISubscription>()
                .ToList();
        }

        public async Task StartAsync(CancellationToken cancellationToken) 
            => await Task.WhenAll(_subscriptions.Select(s => s.StartAsync()));

        /// <summary>
        /// TODO: Graceful Shutdown attempt
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
            => await Task.WhenAll(_subscriptions.Select(s => s.StopAsync()));
    }
}
