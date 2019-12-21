using System.Threading.Tasks;
using System;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Linq;

namespace FluentBus.RabbitMq
{
    internal class RabbitMqSubscription : ISubscription
    {
        #region private fields

        private readonly IServiceProvider _services;
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly RabbitMqSubscriptionOptions _subscriptionOptions;
        private readonly string _queueName;
        private readonly string _subscriptionName;
        private AsyncEventingBasicConsumer _consumer;
        private IModel _consumerChannel;

        #endregion

        #region Constructor

        public RabbitMqSubscription(
            IServiceProvider services,
            IRabbitMQPersistentConnection persistentConnection,
            IOptionsSnapshot<RabbitMqSubscriptionOptions> subscriptionOptions,
            string subscriptionName)
        {
            _services = services;
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
                _consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                _consumer.Received += Consumer_Received;

                _consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: _consumer);
            }
            else
            {
                //_logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;

            try
            {
                _inProgressCount++;
                string messageString = _subscriptionOptions.Encoding.GetString(eventArgs.Body);
                var deserializedMessage = _subscriptionOptions.DeserializationFactory(messageString);

                var message = new RabbitMqMessage(deserializedMessage, eventArgs);
                var msg = _subscriptionOptions.MessageFactory(message);

                using (var scope = _services.CreateScope())
                {
                    await scope.ServiceProvider.GetService<ISubscriptionMediator>().Publish(msg);
                }
            }
            catch (Exception ex)
            {
                //_logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
            }
            finally
            {
                _inProgressCount--;
            }

            // Even on exception we take the message off the queue.
            // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
            // For more information see: https://www.rabbitmq.com/dlx.html
            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopAsync().Wait();
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

        public async Task StopAsync()
        {
            if (_consumer != null)
                _consumer.Received -= Consumer_Received;

            await EnsureClosureOfInProgressTasks();

            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
                _consumerChannel = null;
            }
        }

        #endregion

        #region Graceful Exit

        private int _inProgressCount = 0;

        private async Task EnsureClosureOfInProgressTasks()
        {
            if (_inProgressCount > 0)
                await Observable
                           .Interval(TimeSpan.FromMilliseconds(50))
                           .TakeWhile(x => _inProgressCount > 0);
        }

        #endregion
    }
}
