using System;
using System.Collections.Generic;
using System.Text;

namespace EventBus.Test
{
    public class EventData : IntegrationEvent
    {
        public int TestValue { get; set; }

        public bool TestReturn { get; set; }

        public EventData(int TestValue, bool TestReturn)
        {
            this.TestValue = TestValue;
            this.TestReturn = TestReturn;
        }
    }
}
