using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models.Marketplace
{
    public class ProductOrder
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }
        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}
