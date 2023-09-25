using Newtonsoft.Json;
using NostrSharp.Relay.Enums;
using NostrSharp.Json;

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
