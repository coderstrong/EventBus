using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Test
{
    class EventDataHandler : IIntegrationEventHandler<EventData>
    {
        public static int TestValue { get; set; }
        public Task<bool> Handle(EventData @event)
        {
            TestValue = @event.TestValue;

            return Task.FromResult(true);
        }
    }
}
