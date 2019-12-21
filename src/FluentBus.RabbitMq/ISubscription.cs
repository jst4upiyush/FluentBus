using System.Threading.Tasks;
using System;

namespace FluentBus.RabbitMq
{
    public interface ISubscription : IDisposable
    {
        Task StartAsync();
        Task StopAsync();
    }
}
