using RabbitMQ.Client.Events;

namespace FluentBus.RabbitMq
{
    public interface IRabbitMqMessage
    {
        INotificationMessage Message { get; }

        BasicDeliverEventArgs DeliveryArgs { get; }
    }
}
