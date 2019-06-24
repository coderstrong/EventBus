using System;
using System.Threading.Tasks;

namespace EventBus
{
    public interface IEventBus
    {
        void Subscribe<TIntegrationEvent, TIIntegrationEventHandler>()
            where TIntegrationEvent : IntegrationEvent
            where TIIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>;

        void Subscribe(Type eventType, Type handler);

        void UnSubscribe<TIntegrationEvent, TIIntegrationEventHandler>()
            where TIntegrationEvent : IntegrationEvent
            where TIIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>;

        void UnSubscribeAll<TIntegrationEvent>()
            where TIntegrationEvent : IntegrationEvent;

        void Publish<TIntegrationEvent>(TIntegrationEvent eventData)
            where TIntegrationEvent : IntegrationEvent;

        Task PublishAsync<TIntegrationEvent>(TIntegrationEvent eventData) 
            where TIntegrationEvent : IntegrationEvent;
    }
}
