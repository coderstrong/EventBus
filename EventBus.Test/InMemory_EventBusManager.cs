using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace EventBus.Test
{
    [TestClass]
    public class InMemory_EventBusManager
    {
        [TestMethod]
        public void After_Creation_Should_Be_Empty()
        {
            IEventBusManager _manager = new InMemoryEventBusManager();
            _manager.IsEmpty.ShouldBeTrue();
        }

        [TestMethod]
        public void After_Subscribe_Should_Be_Contain_The_Event()
        {
            IEventBusManager _manager = new InMemoryEventBusManager();
            _manager.AddSubscribe<EventData, EventDataHandler>();
            _manager.HasSubscribeForEvent<EventData>().ShouldBeTrue();
        }

        [TestMethod]
        public void After_Clean_Should_Be_Empty()
        {
            IEventBusManager _manager = new InMemoryEventBusManager();
            _manager.AddSubscribe<EventData, EventDataHandler>();
            _manager.IsEmpty.ShouldBeFalse();

            _manager.Clear();

            _manager.IsEmpty.ShouldBeTrue();
        }
    }
}
