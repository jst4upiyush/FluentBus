using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace FluentBus.IntegrationTests
{
    public interface IPublisher : IDisposable
    {
        Task SendAsync(byte[] message);
    }

    internal class RabbitMqPublisher : IPublisher
    {
        private readonly IConnection _connection;
        private readonly string _exchangeName;
        private readonly string _topic;

        public RabbitMqPublisher(string exchangeName, string topic)
        {
            var factory = new ConnectionFactory { DispatchConsumersAsync = true };
            _connection = factory.CreateConnection();

            _exchangeName = exchangeName;
            _topic = topic;
        }

        public void Dispose() { }

        public async Task SendAsync(byte[] message) 
            => _connection.CreateModel().BasicPublish(_exchangeName, _topic, body: message);
    }
}