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

    internal abstract class NotificationHandlerWrapper
    {
        public abstract Task Handle(INotificationMessage notification, CancellationToken cancellationToken);
    }

    internal class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
        where TNotification : INotificationMessage
    {
        private readonly IServiceProvider _services;

        public NotificationHandlerWrapperImpl(IServiceProvider services)
            => _services = services;

        public override async Task Handle(INotificationMessage notification, CancellationToken cancellationToken)
        {
            var pipeline = _services
                .GetServices<IConsumerPipelineBehavior<TNotification>>()
                .Reverse()
                .ToList();

            var handlerActions = _services
                .GetServices<IMessageConsumer<TNotification>>()
                .Select(consumer => HandlePipeline(pipeline, consumer, (TNotification)notification, cancellationToken))
                .ToList();

            await Task.WhenAll(handlerActions);
        }

        private Task HandlePipeline(
            IEnumerable<IConsumerPipelineBehavior<TNotification>> pipe,
            IMessageConsumer<TNotification> handler,
            TNotification notification,
            CancellationToken cancellationToken)
        {
            Func<Task> Handler = () => handler.Handle(notification, cancellationToken);
            return pipe.Aggregate(Handler, (next, pipeline) => () => pipeline.Handle(notification, cancellationToken, next))();
        }
    }
}
