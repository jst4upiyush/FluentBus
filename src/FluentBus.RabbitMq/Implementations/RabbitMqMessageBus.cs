using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FluentBus.RabbitMq
{
    internal class RabbitMqMessageBus : IMessagePublisher
    {
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IOptionsSnapshot<RabbitMqPublisherOptions> _publisherOptions;

        public RabbitMqMessageBus(IOptionsSnapshot<RabbitMqPublisherOptions> publisherOptions, IRabbitMQPersistentConnection persistentConnection)
        {
            _publisherOptions = publisherOptions;
            _persistentConnection = persistentConnection;
        }

        public Task Publish<TMessage>(TMessage message)
            where TMessage : class
        {
            var messageBusOptions = _publisherOptions.Get(typeof(TMessage).Name);
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var eventName = message.GetType().Name;
            using (var channel = _persistentConnection.CreateModel())
            {
                string exchangeName = messageBusOptions.Exchange ?? message.GetType().Name;

                channel.ExchangeDeclare(exchange: exchangeName, type: messageBusOptions.ExchangeType);

                var body = messageBusOptions.Encoding.GetBytes(messageBusOptions.Serializer(message));

                var properties = channel.CreateBasicProperties();
                messageBusOptions.BasicProperties.Aggregate(properties, (props, action) => { action(props); return props; });

                properties.Type = message.GetType().FullName;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentEncoding = messageBusOptions.Encoding.WebName;
                properties.DeliveryMode = messageBusOptions.DeliveryMode;
                properties.MessageId = Guid.NewGuid().ToString();


                channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            }

            return Task.CompletedTask;
        }
    }
}
