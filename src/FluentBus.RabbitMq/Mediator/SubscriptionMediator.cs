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

        public SubscriptionMediator(IServiceProvider services) => _services = services;

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotificationMessage
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var notificationType = notification.GetType();
            var wrapper = (NotificationHandlerWrapper)Activator.CreateInstance(typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType));

            await wrapper.Handle(notification, cancellationToken, _services);
        }
    }

    internal abstract class NotificationHandlerWrapper
    {
        public abstract Task Handle(INotificationMessage notification, CancellationToken cancellationToken, IServiceProvider services);
    }

    internal class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
        where TNotification : INotificationMessage
    {
        public override async Task Handle(INotificationMessage notification, CancellationToken cancellationToken, IServiceProvider services)
        {
            var pipeline = services
                .GetServices<IConsumerPipelineBehavior<TNotification>>()
                .Reverse()
                .ToList();

            var handlerActions = services
                .GetServices<IMessageConsumer<TNotification>>()
                .Select(x => HandlePipeline(pipeline, x, (TNotification)notification, cancellationToken))
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
