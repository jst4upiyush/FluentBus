using RabbitMQ.Client.Events;

namespace FluentBus.RabbitMq
{
    internal class RabbitMqMessage : IRabbitMqMessage
    {
        public RabbitMqMessage(INotificationMessage message, BasicDeliverEventArgs deliveryArgs)
        {
            Message = message;
            DeliveryArgs = deliveryArgs;
        }

        public INotificationMessage Message { get; }

        public BasicDeliverEventArgs DeliveryArgs { get; }
    }
}
