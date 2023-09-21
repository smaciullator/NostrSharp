using Newtonsoft.Json;
using NostrSharp.Nostr.Enums.Marketplace;
using System.Collections.Generic;

namespace NostrSharp.Nostr.Models.Marketplace
{
    public class PaymentRequest
    {
        [JsonProperty("id")]
        public string id { get; set; }
        [JsonProperty("type")]
        public MarketplaceMessageType Type { get; init; } = MarketplaceMessageType.PaymentRequest;
        [JsonProperty("message")]
        public string? Message { get; set; }
        [JsonProperty("payment_options")]
        public List<PaymentOption> PaymentOptions { get; set; }
    }
}
