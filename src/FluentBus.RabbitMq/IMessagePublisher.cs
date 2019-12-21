using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FluentBus.RabbitMq
{
    public interface IMessagePublisher
    {
        Task Publish<TMessage>(TMessage message)
            where TMessage : class;
    }
}
