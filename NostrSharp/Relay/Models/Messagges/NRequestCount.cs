using Newtonsoft.Json;
using NostrSharp.Relay.Enums;
using NostrSharp.Settings;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NRequestCount : NBaseMessage
    {
        [NArrayElement(1)]
        public string SubscriptionId { get; set; }
        [NArrayElement(2)]
        public NSRelayFilter? Filters { get; set; }


        public NRequestCount() { }
        public NRequestCount(string subscriptionId) : this(subscriptionId, null) { }
        public NRequestCount(string subscriptionId, NSRelayFilter? filters)
        {
            MessageType = NMessageTypes.Count;
            SubscriptionId = subscriptionId;
            Filters = filters;
        }
    }
}
