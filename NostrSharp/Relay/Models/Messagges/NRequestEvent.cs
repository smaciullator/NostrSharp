using Newtonsoft.Json;
using NostrSharp.Nostr;
using NostrSharp.Relay.Enums;
using NostrSharp.Settings;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NRequestEvent : NBaseMessage
    {
        [NArrayElement(1)]
        public NEvent? Event { get; set; }


        public NRequestEvent() { }
        public NRequestEvent(NEvent ev)
        {
            MessageType = NMessageTypes.Event;
            Event = ev;
        }
    }
}
