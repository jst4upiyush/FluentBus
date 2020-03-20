using NUnit.Framework;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentBus.IntegrationTests
{
    public class RabbitMqPubSubTests
    {
        private IModel SetupAutoDeleteQueue(string queueName, bool autoDelete = true)
        {
            var channel = new ConnectionFactory()
                           .CreateConnection()
                           .CreateModel();
            channel.QueueDeclare(queueName, true, false, autoDelete);
            return channel;
        }

        [Test]
        public async Task BasicPubSubTests()
        {
            string queueName = "q.users";
            SetupAutoDeleteQueue(queueName);
            var factory = new PublisherFactory {
                { "users", () => new RabbitMqPublisher(string.Empty, queueName) }
            };

            IMessageSubscription subscriptions = new MessageSubscription {
                { "users", () => new RabbitMqConsumer(queueName) }
            };

            using (IPublisher publisher = factory.Get("users"))
            using (subscriptions)
            using (ManualResetEventSlim resetEvent = new ManualResetEventSlim())
            {
                string content = Guid.NewGuid().ToString();
                string actual = string.Empty;

                subscriptions.Subscribe("users", async received =>
                {
                    actual = Encoding.UTF8.GetString(received);
                    resetEvent.Set();
                });

                await publisher.SendAsync(Encoding.UTF8.GetBytes(content));
                resetEvent.Wait(TimeSpan.FromSeconds(2));
                Assert.IsTrue(resetEvent.IsSet);
                Assert.AreEqual(content, actual);
            }
        }

        [Test]
        public async Task UnsubscribeTests()
        {
            string queueName = "q.users";
            SetupAutoDeleteQueue(queueName);
            var factory = new PublisherFactory {
                    { "users", () => new RabbitMqPublisher(string.Empty, queueName) }
                };

            IMessageSubscription subscriptions = new MessageSubscription {
                    { "users", () => new RabbitMqConsumer(queueName) }
                };

            using (IPublisher publisher = factory.Get("users"))
            using (subscriptions)
            using (ManualResetEventSlim resetEvent = new ManualResetEventSlim())
            {
                string content = Guid.NewGuid().ToString();
                string actual = string.Empty;

                subscriptions.Subscribe("users", async received =>
                {
                    actual = Encoding.UTF8.GetString(received);
                    resetEvent.Set();
                });

                await publisher.SendAsync(Encoding.UTF8.GetBytes(content));
                resetEvent.Wait(TimeSpan.FromSeconds(2));
                Assert.IsTrue(resetEvent.IsSet);

                resetEvent.Reset();
                subscriptions.UnSubscribe("users");
                await publisher.SendAsync(Encoding.UTF8.GetBytes(content));
                resetEvent.Wait(TimeSpan.FromSeconds(2));

                Assert.IsFalse(resetEvent.IsSet);
            }
        }
    }
}