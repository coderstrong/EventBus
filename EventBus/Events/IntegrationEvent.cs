using Newtonsoft.Json;
using System;

namespace EventBus
{
    public class IntegrationEvent
    {
        [JsonConstructor]
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }

        [JsonProperty]
        public Guid Id { get; }

        [JsonProperty]
        public DateTime CreationDate { get; }

        [JsonProperty]
        public string JwtToken { get; set; }

        [JsonProperty]
        public byte DeliveryMode { get; set; }
    }
}
