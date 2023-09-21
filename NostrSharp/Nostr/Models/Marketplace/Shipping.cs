using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models.Marketplace
{
    public class Shipping
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("cost")]
        public float BaseCost { get; set; }
        [JsonProperty("regions")]
        public string[]? Regions { get; set; }
    }
}
