using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBus.RabbitMq
{
    internal class SubscriptionMediator : ISubscriptionMediator
    {
        private readonly IServiceProvider _services;

        public SubscriptionMediator(IServiceProvider services)
            => _services = services;

        public async Task Publish(
            INotificationMessage notification,
            CancellationToken cancellationToken = default)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var notificationType = notification.GetType();
            var wrapper = (NotificationHandlerWrapper)_services.GetService(typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType));

            await wrapper.Handle(notification, cancellationToken);
        }
    }

}
