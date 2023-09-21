using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace NostrSharp.Nostr.Models.Marketplace
{
    public class Product
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("stall_id")]
        public string StallId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("images")]
        public string[] Images { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
        [JsonProperty("price")]
        public float Price { get; set; }
        [JsonProperty("quantity")]
        public int Quantity { get; set; }
        [JsonProperty("specs")]
        public string[][] _specs
        {
            get => Specifications is not null ? Specifications.Select(x => new string[2] { x.Key, x.Value }).ToArray() : new string[0][];
            set
            {
                Dictionary<string, string> specs = new();
                foreach (string[] spec in value)
                    if (spec.Length == 2)
                        specs.TryAdd(spec[0], spec[1]);
                Specifications = specs;
            }
        }
        [JsonIgnore]
        public Dictionary<string, string> Specifications { get; set; }
        [JsonProperty("shipping")]
        public Shipping Shipping
        {
            get => _shipping;
            set
            {
                value.Name = null;
                value.Regions = null;
                _shipping = value;
            }
        }
        [JsonIgnore]
        private Shipping _shipping { get; set; }
    }
}
