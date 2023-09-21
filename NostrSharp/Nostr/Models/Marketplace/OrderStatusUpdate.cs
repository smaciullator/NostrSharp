using Newtonsoft.Json;
using NostrSharp.Nostr.Enums.Marketplace;

namespace NostrSharp.Nostr.Models.Marketplace
{
    public class OrderStatusUpdate
    {
        [JsonProperty("id")]
        public string id { get; set; }
        [JsonProperty("type")]
        public MarketplaceMessageType Type { get; init; } = MarketplaceMessageType.OrderStatusUpdate;
        [JsonProperty("message")]
        public string? Message { get; set; }
        [JsonProperty("paid")]
        public bool Paid { get; set; }
        [JsonProperty("shipped")]
        public bool Shipped { get; set; }
    }
}
