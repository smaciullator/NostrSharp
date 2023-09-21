using Newtonsoft.Json;

namespace NostrSharp.Relay.Models.Messagges
{
    public class CountResult
    {
        [JsonProperty(propertyName: "count")]
        public long Count { get; set; }
        [JsonProperty(propertyName: "approximate")]
        public bool Approximate { get; set; }
    }
}
