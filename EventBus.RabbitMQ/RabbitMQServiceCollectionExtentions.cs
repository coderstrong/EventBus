using Microsoft.Extensions.DependencyInjection;
using System;

namespace EventBus.RabbitMQ
{
    public static class MongoDBServiceCollectionExtentions
    {
        public static IServiceCollection AddEventBusRabbitMQ(this IServiceCollection services, Action<RabbitMQOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.Configure(setupAction);

            services.AddHttpContextAccessor();
            services.AddSingleton<IEventBusManager, InMemoryEventBusManager>();
            services.AddTransient<IRabbitMQPersistentConnection, RabbitMQPersistentConnection>();
            services.AddTransient<IEventBus, EventBusRabbitMQ>();

            return services;
        }
    }
}