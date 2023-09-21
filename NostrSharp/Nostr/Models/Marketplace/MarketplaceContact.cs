using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models.Marketplace
{
    public class MarketplaceContact
    {
        [JsonProperty("nostr")]
        public string NPub { get; set; }
        [JsonProperty("phone")]
        public string? Phone { get; set; }
        [JsonProperty("email")]
        public string? Email { get; set; }
    }
}
