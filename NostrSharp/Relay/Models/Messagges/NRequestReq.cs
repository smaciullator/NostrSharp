using Newtonsoft.Json;
using NostrSharp.Relay.Enums;
using NostrSharp.Settings;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NRequestReq : NBaseMessage
    {
        [NArrayElement(1)]
        public string SubscriptionId { get; set; }
        [NArrayElement(2)]
        public NSRelayFilter? Filter1 { get; set; }
        [NArrayElement(3)]
        public NSRelayFilter? Filter2 { get; set; }
        [NArrayElement(4)]
        public NSRelayFilter? Filter3 { get; set; }
        [NArrayElement(5)]
        public NSRelayFilter? Filter4 { get; set; }


        public NRequestReq() { }
        public NRequestReq(string subscriptionId) : this(subscriptionId, null) { }
        public NRequestReq(string subscriptionId, NSRelayFilter filters)
        {
            MessageType = NMessageTypes.Request;
            SubscriptionId = subscriptionId;
            Filter1 = filters;
        }
        public NRequestReq(string subscriptionId, NSRelayFilter filter1, NSRelayFilter filter2)
        {
            MessageType = NMessageTypes.Request;
            SubscriptionId = subscriptionId;
            Filter1 = filter1;
            Filter2 = filter2;
        }
        public NRequestReq(string subscriptionId, NSRelayFilter filter1, NSRelayFilter filter2, NSRelayFilter filter3)
        {
            MessageType = NMessageTypes.Request;
            SubscriptionId = subscriptionId;
            Filter1 = filter1;
            Filter2 = filter2;
            Filter3 = filter3;
        }
        public NRequestReq(string subscriptionId, NSRelayFilter filter1, NSRelayFilter filter2, NSRelayFilter filter3, NSRelayFilter filter4)
        {
            MessageType = NMessageTypes.Request;
            SubscriptionId = subscriptionId;
            Filter1 = filter1;
            Filter2 = filter2;
            Filter3 = filter3;
            Filter4 = filter4;
        }
    }
}
