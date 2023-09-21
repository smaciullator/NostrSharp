using Newtonsoft.Json;
using NostrSharp.Nostr.Enums.Marketplace;
using System;

namespace NostrSharp.Nostr.Models.Marketplace
{
    public class PaymentOption
    {
        [JsonProperty("type")]
        public string Type { get; private set; }
        [JsonIgnore]
        public PaymentType PaymentType
        {
            get
            {
                Enum.TryParse<PaymentType>(Type, out PaymentType result);
                return result;
            }
            set => Type = value.ToString();
        }

        [JsonProperty("link")]
        public string Link { get; set; }
    }
}
