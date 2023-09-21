using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models.Marketplace
{
    public class Stall
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
        [JsonProperty("shipping")]
        public Shipping Shipping { get; set; }
    }
}
