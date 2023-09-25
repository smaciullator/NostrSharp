using Newtonsoft.Json;
using NostrSharp.Json;
using System;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NBaseMessage
    {
        [NArrayElement(0)]
        public string? MessageType { get; init; }
        [JsonIgnore]
        public DateTime ReceivedTimestamp { get; internal set; } = DateTime.UtcNow;


        public NBaseMessage() { }
    }
}
