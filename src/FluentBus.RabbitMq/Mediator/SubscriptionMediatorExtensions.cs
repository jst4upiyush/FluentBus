using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentBus.RabbitMq
{
    internal static class SubscriptionMediatorExtensions
    {
        public static IServiceCollection AddSubscriptionMediator(this IServiceCollection services)
        {
            services.TryAddSingleton<ISubscriptionMediator, SubscriptionMediator>();
            return services;
        }
    }
}
