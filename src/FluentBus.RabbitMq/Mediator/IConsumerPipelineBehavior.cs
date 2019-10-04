using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentBus.RabbitMq
{
    public interface IConsumerPipelineBehavior<TNotification>
        where TNotification : INotificationMessage
    {
        /// <summary>
        /// Pipeline handler. Perform any additional behavior and await the <paramref name="next"/> delegate as necessary
        /// </summary>
        /// <param name="request">Incoming request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="next">Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the handler.</param>
        /// <returns>Awaitable task returning the <typeparamref name="TResponse"/></returns>
        Task Handle(TNotification notification, CancellationToken cancellationToken, Func<Task> next);
    }
}
