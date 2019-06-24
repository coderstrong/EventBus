using System.Threading.Tasks;

namespace EventBus
{
    public interface IIntegrationEventHandler
    {
    }

    public interface IIntegrationEventHandler<TIntegrationEvent> : IIntegrationEventHandler where TIntegrationEvent : IntegrationEvent
    {
        Task<bool> Handle(TIntegrationEvent @event);
    }
}
