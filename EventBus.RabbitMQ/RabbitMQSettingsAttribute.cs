using System;
using System.Collections.Generic;

namespace EventBus.RabbitMQ
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RabbitMQSettingsAttribute : Attribute
    {
        public string ExchangeName { get; set; }

        public string ExchangeType { get; set; } = "direct";

        public string QueueName { get; set; }

        public string RootingKey { get; set; }

        public int RetryCount { get; set; } = 3;

        public bool Druable { get; set; } = false;

        /// <summary>
        /// The exclusive flag can be set to true to request the consumer to be the only one on the target queue
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// Auto remove true it mean queue will be remove when the last consumer disconnect
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// If needs to know if they reached at least one queue set true
        /// </summary>
        public bool Mandatory { get; set; } = false;

        public Dictionary<string, object> Args { get; set; } = null;
    }
}
