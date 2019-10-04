using System.Threading.Tasks;
using System;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;

namespace FluentBus.RabbitMq
{
    public interface ISubscription : IDisposable
    {
        Task StartAsync();
        Task StopAsync();
    }

    internal class RabbitMqSubscription : ISubscription
    {
        #region private fields

        private readonly ISubscriptionMediator _mediator;
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly RabbitMqSubscriptionOptions _subscriptionOptions;
        private IModel _consumerChannel;
        private readonly string _queueName;
        private readonly string _subscriptionName;

        #endregion

        #region Constructor

        public RabbitMqSubscription(
            ISubscriptionMediator mediator,
            IRabbitMQPersistentConnection persistentConnection,
            IOptionsSnapshot<RabbitMqSubscriptionOptions> subscriptionOptions,
            string subscriptionName)
        {
            _mediator = mediator;
            _persistentConnection = persistentConnection;
            _subscriptionName = subscriptionName;

            _subscriptionOptions = subscriptionOptions.Get(_subscriptionName);
            _queueName = _subscriptionOptions.Queue;
        }

        #endregion

        #region Private Subscription Helpers

        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var channel = _persistentConnection.CreateModel();

            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            if (!string.IsNullOrEmpty(_subscriptionOptions.Exchange)
                && !string.IsNullOrEmpty(_subscriptionOptions.ExchangeType)
                && !string.IsNullOrEmpty(_subscriptionOptions.RoutingKey))
            {
                channel.ExchangeDeclare(exchange: _subscriptionOptions.Exchange,
                                        type: _subscriptionOptions.ExchangeType);

                channel.QueueBind(queue: _queueName,
                                  exchange: _subscriptionOptions.Exchange,
                                  routingKey: _subscriptionOptions.RoutingKey);
            }

            channel.CallbackException += (sender, ea) =>
            {
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };

            return channel;
        }

        private void StartBasicConsume()
        {
            if (_consumerChannel == null)
            {
                _consumerChannel = CreateConsumerChannel();
            }

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += Consumer_Received;

                _consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);
            }
            else
            {
                //_logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = _subscriptionOptions.Encoding.GetString(eventArgs.Body);

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                //_logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
            }

            // Even on exception we take the message off the queue.
            // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
            // For more information see: https://www.rabbitmq.com/dlx.html
            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            var msg = JsonConvert.DeserializeObject(message, _subscriptionOptions.MessageType) as INotificationMessage;
            await _mediator.Publish(msg);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
                _consumerChannel = null;
            }
        }
        #endregion

        #region ISubscription Implementation

        public Task StartAsync()
        {
            if (_consumerChannel != null)
            {
                throw new Exception("Cannot Start an already running Subscription");
            }

            CreateConsumerChannel();
            StartBasicConsume();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _consumerChannel.Dispose();
            _consumerChannel = null;
            return Task.CompletedTask;
        }

        #endregion
    }
}
