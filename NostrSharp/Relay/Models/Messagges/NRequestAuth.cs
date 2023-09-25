using Newtonsoft.Json;
using NostrSharp.Nostr;
using NostrSharp.Relay.Enums;
using NostrSharp.Json;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NRequestAuth : NBaseMessage
    {
        [NArrayElement(1)]
        public NEvent AuthEvent { get; set; }


        public NRequestAuth() { }
        public NRequestAuth(NEvent ev)
        {
            MessageType = NMessageTypes.Authentication;
            AuthEvent = ev;
        }
    }
}
