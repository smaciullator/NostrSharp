using Newtonsoft.Json;
using NostrSharp.Nostr.Enums.Marketplace;

namespace NostrSharp.Nostr.Models.Marketplace
{
    public class CustomerOrder
    {
        [JsonProperty("id")]
        public string id { get; set; }
        [JsonProperty("type")]
        public MarketplaceMessageType Type { get; init; } = MarketplaceMessageType.NewOrder;
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("address")]
        public string? Address { get; set; }
        [JsonProperty("message")]
        public string? Message { get; set; }
        [JsonProperty("contact")]
        public MarketplaceContact Contact { get; set; }
        [JsonProperty("items")]
        public ProductOrder Items { get; set; }
        [JsonProperty("shipping_id")]
        public string ShippingId { get; set; }
    }
}
