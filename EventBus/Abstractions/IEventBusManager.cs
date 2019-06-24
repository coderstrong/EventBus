using System;
using System.Collections.Generic;

namespace EventBus
{
    public interface IEventBusManager
    {
        void AddSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        void AddSubscribe(Type eventData, Type eventHandler);
        void RemoveSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        void RemoveSubscribe(Type eventData, Type eventHandler);
        bool HasSubscribeForEvent<T>() where T : IntegrationEvent;
        bool HasSubscribeForEvent(Type eventData);
        IEnumerable<Type> GetHandlersForEvent<T>() where T : IntegrationEvent;
        IEnumerable<Type> GetHandlersForEvent(Type eventData);

        Type GetEventTypeByName(string eventName);
        bool IsEmpty { get; }
        void Clear();
    }
}
