using Newtonsoft.Json;
using System.Collections.Generic;

namespace NostrSharp.Relay.Models
{
    public class RelayNIP11Metadata
    {
        [JsonProperty(propertyName: "contact")]
        public string Contact { get; set; }

        [JsonProperty(propertyName: "description")]
        public string Description { get; set; }

        [JsonProperty(propertyName: "name")]
        public string Name { get; set; }

        [JsonProperty(propertyName: "pubkey")]
        public string PubKey { get; set; }

        [JsonProperty(propertyName: "software")]
        public string Software { get; set; }

        [JsonProperty(propertyName: "supported_nips")]
        public List<int> SupportedNips { get; set; }

        [JsonProperty(propertyName: "version")]
        public string Version { get; set; }
    }
}
