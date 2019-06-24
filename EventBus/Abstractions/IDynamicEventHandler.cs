using System.Threading.Tasks;

namespace EventBus
{
    public interface IDynamicEventHandler
    {
        Task<bool> Handle(dynamic @event);
    }
}
