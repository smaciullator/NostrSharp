using Newtonsoft.Json;
using System.Collections.Generic;

namespace NostrSharp.Models
{
    public class NIP05Response
    {
        [JsonProperty("names")]
        public Dictionary<string, string>? Names { get; init; }
        [JsonProperty("relays")]
        public Dictionary<string, string[]>? Relays { get; init; }
    }
}
