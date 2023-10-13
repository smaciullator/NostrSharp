using Newtonsoft.Json;
using NostrSharp.Json;
using NostrSharp.Relay.Enums;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NRequestClose : NBaseMessage
    {
        [NArrayElement(1)]
        public string SubscriptionId { get; set; }


        public NRequestClose() { }
        public NRequestClose(string subscriptionId)
        {
            MessageType = NMessageTypes.Close;
            SubscriptionId = subscriptionId;
        }
    }
}
