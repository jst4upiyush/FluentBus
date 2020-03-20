using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FluentBus.Core
{
    public sealed class RabbitMQConsumerClient : IConsumerClient
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly string _exchangeName;
        private readonly string _queueName;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private IModel _channel;

        private IConnection _connection;

        public RabbitMQConsumerClient(string queueName,
            IConnectionChannelPool connectionChannelPool,
            RabbitMQOptions options)
        {
            _queueName = queueName;
            _connectionChannelPool = connectionChannelPool;
            _rabbitMQOptions = options;
            _exchangeName = connectionChannelPool.Exchange;
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public BrokerAddress BrokerAddress => new BrokerAddress("RabbitMQ", _rabbitMQOptions.HostName);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            Connect();

            foreach (var topic in topics)
            {
                _channel.QueueBind(_queueName, _exchangeName, topic);
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnConsumerReceived;
            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

            _channel.BasicConsume(_queueName, false, consumer);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }

            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit(object sender)
        {
            _channel.BasicAck((ulong)sender, false);
        }

        public void Reject(object sender)
        {
            _channel.BasicReject((ulong)sender, true);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }

        public void Connect()
        {
            if (_connection != null)
            {
                return;
            }

            _connectionLock.Wait();

            try
            {
                if (_connection == null)
                {
                    _connection = _connectionChannelPool.GetConnection();

                    _channel = _connection.CreateModel();

                    _channel.ExchangeDeclare(_exchangeName, RabbitMQOptions.ExchangeType, true);

                    var arguments = new Dictionary<string, object>
                    {
                        {"x-message-ttl", _rabbitMQOptions.QueueMessageExpires}
                    };
                    _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        #region events

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e)
        {
        }

        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e)
        {
        }

        private void OnConsumerRegistered(object sender, ConsumerEventArgs e)
        {
        }

        private void OnConsumerShutdown(object sender, ShutdownEventArgs e)
        {
        }

        private void OnConsumerReceived(object sender, BasicDeliverEventArgs e)
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in e.BasicProperties.Headers)
            {
                headers.Add(header.Key, header.Value == null ? null : Encoding.UTF8.GetString((byte[])header.Value));
            }

            var message = new TransportMessage(headers, e.Body);

            OnMessageReceived?.Invoke(e.DeliveryTag, message);
        }

        #endregion
    }
}
