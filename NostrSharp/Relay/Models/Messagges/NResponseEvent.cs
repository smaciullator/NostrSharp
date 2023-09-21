using Newtonsoft.Json;
using NostrSharp.Nostr;
using NostrSharp.Settings;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NResponseEvent : NBaseMessage
    {
        [NArrayElement(1)]
        public string? SubscriptionId { get; set; }
        [NArrayElement(2)]
        public NEvent? Event { get; set; }


        public NResponseEvent() { }
    }
}
