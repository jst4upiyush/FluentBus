using FluentBus.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FluentBus.Examples
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder().ConfigureServices((hostContext, services) =>
            {
                services.AddTransient(typeof(IConsumerPipelineBehavior<>), typeof(LoggingBehavior<>));

                services.AddTransient<IMessageConsumer<UserCreated>, UserCreatedConsumer>();
                services.AddTransient<IMessageConsumer<UserDeleted>, UserDeletedConsumer>();

                var messageBusBuilder = services.AddRabbitMqBus(config => config.Connection = "amqp://guest:guest@localhost:5672");

                messageBusBuilder
                    .Subscriptions
                        .ConfigureUserCreatedSubscriptions()
                        .ConfigureUserDeletedSubscriptions();
                messageBusBuilder
                    .Publishers
                        .Defaults(config =>
                        {
                            config.Exchange = "EX.Events.User";
                            config.ExchangeType = ExchangeType.Topic;
                        })
                        .ConfigureUserCreatedPublisher()
                        .Configure<UserDeleted>(config => config.RoutingKey = nameof(UserDeleted));

                services.AddSingleton<IHostedService, Startup>();
            })
            .Build();

            await host.RunAsync();

            Console.WriteLine("Hello World!");
        }
    }

    public class Startup : IHostedService
    {
        private readonly IMessagePublisher _messageBus;

        public Startup(IMessagePublisher messageBus) 
            => _messageBus = messageBus;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _messageBus.Publish(new UserCreated { Id = 1, Name = "Piyush Rathi" });
            _messageBus.Publish(new UserCreated { Id = 2, Name = "Piyush Rathi" });
            _messageBus.Publish(new UserDeleted { Id = 1 });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    #region Entities

    public class UserCreated : INotificationMessage
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class UserDeleted : INotificationMessage
    {
        public int Id { get; set; }
    }

    #endregion
    
    #region Subscription Module Extensions

    public static class UserSubscriptionModule
    {
        public static SubscriptionBuilder ConfigureUserCreatedSubscriptions(this SubscriptionBuilder builder)
            => builder
                    .Add<UserCreated>(config =>
                    {
                        config.Queue = nameof(UserCreated);
                        config.Exchange = "EX.Events.User";
                        config.ExchangeType = ExchangeType.Topic;
                        config.RoutingKey = nameof(UserCreated);
                        config.Encoding = Encoding.Unicode;
                        config.DeserializationFactory = msg => (UserCreated)new XmlSerializer(typeof(UserCreated)).Deserialize(new StringReader(msg));
                    });

        public static SubscriptionBuilder ConfigureUserDeletedSubscriptions(this SubscriptionBuilder builder)
            => builder
                    .Add<UserDeleted>(config =>
                    {
                        config.Queue = nameof(UserDeleted);
                        config.Exchange = "EX.Events.User";
                        config.ExchangeType = ExchangeType.Topic;
                        config.RoutingKey = nameof(UserDeleted);
                    });
    }

    #endregion

    #region Publisher Module Extensions

    public static class PublisherModule
    {
        public static PublisherBuilder ConfigureUserCreatedPublisher(this PublisherBuilder builder)
            => builder
                .Configure<UserCreated>(config =>
                {
                    config.RoutingKey = nameof(UserCreated);
                    config.Encoding = Encoding.Unicode;
                    config.Serializer = msg => msg.ToXML();
                });

        public static string ToXML<T>(this T msg)
        {
            using (var stringwriter = new StringWriter())
            {
                var serializer = new XmlSerializer(msg.GetType());
                serializer.Serialize(stringwriter, msg);
                return stringwriter.ToString();
            }
        }
    }

    #endregion

    #region Consumers

    public class UserCreatedConsumer : IMessageConsumer<UserCreated>
    {
        public async Task Handle(UserCreated user, CancellationToken cancellationToken)
        {
            await Task.Delay(500);
            Console.WriteLine($"UserCreatedHandler :: {user.Id} : {user.Name}");
            await Task.Delay(500);
        }
    }
    public class UserDeletedConsumer : IMessageConsumer<UserDeleted>
    {
        public Task Handle(UserDeleted user, CancellationToken cancellationToken)
        {
            Console.WriteLine($"UserDeletedHandler :: {user.Id}");
            return Task.CompletedTask;
        }
    }

    #endregion

    #region Pipeline Behaviors

    public class LoggingBehavior<TNotification> : IConsumerPipelineBehavior<TNotification>
        where TNotification : INotificationMessage
    {
        public async Task Handle(TNotification notification, CancellationToken cancellationToken, Func<Task> next)
        {
            Console.WriteLine($"Before Execution of {typeof(TNotification)}");
            await next();
            Console.WriteLine($"After Execution of {typeof(TNotification)}");
        }
    }

    #endregion
}
