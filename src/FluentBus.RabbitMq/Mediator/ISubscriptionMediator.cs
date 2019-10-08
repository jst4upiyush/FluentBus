using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentBus.RabbitMq
{
    internal interface ISubscriptionMediator
    {
        /// <summary>
        /// Asynchronously send a notification to multiple handlers
        /// </summary>
        /// <param name="notification">Notification object</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task that represents the publish operation.</returns>
        Task Publish(INotificationMessage notification, CancellationToken cancellationToken = default);
    }
}
