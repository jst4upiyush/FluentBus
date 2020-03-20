using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FluentBus.IntegrationTests
{
    public interface IMessageSubscription: IDisposable
    {
        void Subscribe(string consumerName, Action<byte[]> callback);

        void UnSubscribe(string consumerName);
    }

    public class MessageSubscription : Dictionary<string, Func<IConsumer>>, IMessageSubscription
    {
        private bool _isDisposing = false;
        private bool _isDisposed = false;
        private readonly ConcurrentDictionary<string, IConsumer> _consumers = new ConcurrentDictionary<string, IConsumer>();

        public void Subscribe(string consumerName, Action<byte[]> callback)
        {
            if (_isDisposed) throw new Exception("Subscriptions are already disposed");
            if (_isDisposing) throw new Exception("Subscriptions are getting disposed");

            if (_consumers.ContainsKey(consumerName))
                throw new Exception($"Subscription already exists on {consumerName}");

            var consumer = _consumers.GetOrAdd(consumerName, this[consumerName]());
            consumer.ConsumeAsync(callback);
        }

        public void UnSubscribe(string consumerName)
        {
            if (!_consumers.ContainsKey(consumerName))
                throw new Exception("Consumer does not exist");

            if (_consumers.TryGetValue(consumerName, out IConsumer consumer))
            {
                consumer.Dispose();
            }

            _consumers.TryRemove(consumerName, out IConsumer _);
        }

        public void Dispose()
        {
            _isDisposing = true;
            _consumers.Keys.ToList().ForEach(UnSubscribe);
            _isDisposing = true;
        }
    }

    public interface IMessageConsumer
    {
        void OnMessage(byte[] message);
    }
}