using Newtonsoft.Json;
using NostrSharp.Json;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NResponseCount : NBaseMessage
    {
        [NArrayElement(1)]
        public string SubscriptionId { get; set; }
        [NArrayElement(1)]
        public CountResult Result { get; set; }


        public NResponseCount() { }
    }
}
