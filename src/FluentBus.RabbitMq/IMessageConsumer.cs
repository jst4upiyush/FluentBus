using System.Threading;
using System.Threading.Tasks;

namespace FluentBus.RabbitMq
{
    /// <summary>
    /// Defines a handler for a notification
    /// </summary>
    /// <typeparam name="TNotification">The type of notification being handled</typeparam>
    public interface IMessageConsumer<in TNotification>
        where TNotification : INotificationMessage
    {
        /// <summary>
        /// Handles a notification
        /// </summary>
        /// <param name="notification">The notification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Marker interface to represent a notification
    /// </summary>
    public interface INotificationMessage { }
}
