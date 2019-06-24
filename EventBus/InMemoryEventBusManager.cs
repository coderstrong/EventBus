using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace EventBus
{
    public class InMemoryEventBusManager : IEventBusManager
    {
        private static readonly object LockObj = new object();

        private readonly ConcurrentDictionary<Type, List<Type>> _eventAndHandlerMapping;

        public InMemoryEventBusManager()
        {
            _eventAndHandlerMapping = new ConcurrentDictionary<Type, List<Type>>();
        }
        public void AddSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            AddSubscribe(typeof(T), typeof(TH));
        }

        public void AddSubscribe(Type eventData, Type eventHandler)
        {
            lock (LockObj)
            {
                if (!HasSubscribeForEvent(eventData))
                {
                    var handlers = new List<Type>();
                    _eventAndHandlerMapping.TryAdd(eventData, handlers);
                }

                if (_eventAndHandlerMapping[eventData].All(h => h != eventHandler))
                {
                    _eventAndHandlerMapping[eventData].Add(eventHandler);
                }
            }
        }

        public void RemoveSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            var handlerToRemove = FindSubscribeToRemove(typeof(T), typeof(TH));
            RemoveSubscribe(typeof(T), handlerToRemove);
        }

        public void RemoveSubscribe(Type eventData, Type eventHandler)
        {
            if (eventHandler != null)
            {
                lock (LockObj)
                {
                    _eventAndHandlerMapping[eventData].Remove(eventHandler);
                    if (!_eventAndHandlerMapping[eventData].Any())
                    {
                        _eventAndHandlerMapping.TryRemove(eventData, out List<Type> removedHandlers);
                    }
                }
            }
        }

        private Type FindSubscribeToRemove(Type eventData, Type eventHandler)
        {
            if (!HasSubscribeForEvent(eventData))
            {
                return null;
            }

            return _eventAndHandlerMapping[eventData].FirstOrDefault(eh => eh == eventHandler);
        }

        public bool HasSubscribeForEvent<T>() where T : IntegrationEvent
        {
            return _eventAndHandlerMapping.ContainsKey(typeof(T));
        }

        public bool HasSubscribeForEvent(Type eventData)
        {
            return _eventAndHandlerMapping.ContainsKey(eventData);
        }

        public IEnumerable<Type> GetHandlersForEvent<T>() where T : IntegrationEvent
        {
            return GetHandlersForEvent(typeof(T));
        }

        public IEnumerable<Type> GetHandlersForEvent(Type eventData)
        {
            if (HasSubscribeForEvent(eventData))
            {
                return _eventAndHandlerMapping[eventData];
            }

            return new List<Type>();
        }

        public Type GetEventTypeByName(string eventName)
        {
            return _eventAndHandlerMapping.Keys.FirstOrDefault(eh => eh.Name == eventName);
        }

        public bool IsEmpty => !_eventAndHandlerMapping.Keys.Any();

        public void Clear() => _eventAndHandlerMapping.Clear();

    }
}
