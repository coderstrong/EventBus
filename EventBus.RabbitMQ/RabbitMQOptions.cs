using System.Collections.Generic;

namespace EventBus.RabbitMQ
{
    public class RabbitMQOptions
    {
        /// <summary>
        /// Host string
        /// </summary>
        public string HostName { set; get; } = "localhost";

        /// <summary>
        /// Port connect
        /// </summary>
        public int Port { set; get; } = 5672;

        /// <summary>
        /// Username connect
        /// </summary>
        public string UserName { set; get; }

        /// <summary>
        /// Password for connect
        /// </summary>
        public string Password { set; get; }

        /// <summary>
        /// Retry number
        /// </summary>
        public int RetryCount { set; get; }

        /// <summary>
        /// Exchange name
        /// </summary>
        public string ExchangeName { set; get; } = "vtvpay_event_bus";

        /// <summary>
        /// Exchange name
        /// </summary>
        public string ExchangeType { set; get; } = "direct";

        /// <summary>
        /// Queue name
        /// </summary>
        public string QueueName { set; get; }

        /// <summary>
        /// This flag for queue is save in memory or disk
        /// </summary>
        public bool Druable { get; set; } = false;

        /// <summary>
        /// The exclusive flag can be set to true to request the consumer to be the only one on the target queue
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// Queue auto remove when the last consumer discomnect
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        public IDictionary<string, object> Args { get; set; } = null;

        /// <summary>
        /// Using config lib is Consumer
        /// </summary>
        public bool IsConsummer { get; set; } = false;
    }
}
