using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FluentBus.IntegrationTests
{
    public interface IConsumer : IDisposable
    {
        Task ConsumeAsync(Action<byte[]> callback);
    }

    internal class RabbitMqConsumer : IConsumer
    {
        private readonly IConnection _connection;
        private readonly IModel _model;
        private readonly AsyncEventingBasicConsumer _consumer;
        private Action<byte[]> _callback;
        private readonly string _queueName;
        public RabbitMqConsumer(string queueName)
        {
            _queueName = queueName;
            var factory = new ConnectionFactory { DispatchConsumersAsync = true };
            _connection = factory.CreateConnection();
            _model = _connection.CreateModel();
            _consumer = new AsyncEventingBasicConsumer(_model);
        }

        public async Task ConsumeAsync(Action<byte[]> callback)
        {
            _callback = callback;
            _consumer.Received += OnConsumerReceived;

            _model.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: _consumer);
        }

        private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs e) 
            => _callback(e.Body);

        public void Dispose()
        {
            _consumer.Received -= OnConsumerReceived;
            _model.Dispose();
            _connection.Dispose();
        }
    }
}