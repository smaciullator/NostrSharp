using Newtonsoft.Json;
using NostrSharp.Settings;
using System.Collections.Generic;

namespace NostrSharp.Models.LN
{
    public class LNPayEndpointResponse
    {
        [JsonProperty(propertyName: "callback")]
        public string Callback { get; set; }

        [JsonProperty(propertyName: "maxSendable")]
        public long MaxSendable { get; set; }

        [JsonProperty(propertyName: "minSendable")]
        public long MinSendable { get; set; }

        /// <summary>
        /// [[\"text/plain\",\"Pay to Wallet of Satoshi user: dynamicclick68\"],[\"text/identifier\",\"dynamicclick68@walletofsatoshi.com\"]]
        /// </summary>
        [JsonProperty(propertyName: "metadata")]
        public string? _metadata { get; set; }

        [JsonIgnore]
        public LNPayEndpointResponse_Metadata Metadata => string.IsNullOrEmpty(_metadata) ? new() : JsonConvert.DeserializeObject<LNPayEndpointResponse_Metadata>(_metadata, SerializerCustomSettings.Settings) ?? new();
        //[JsonIgnore]
        //public string? Metadata1 => string.IsNullOrEmpty(Metadata) ? null : Metadata[0][1];
        //[JsonIgnore]
        //public string? Metadata2 => string.IsNullOrEmpty(Metadata) ? null : Metadata[1][1];

        [JsonProperty(propertyName: "commentAllowed")]
        public string CommentAllowed { get; set; }

        [JsonProperty(propertyName: "tag")]
        public string Tag { get; set; }

        [JsonProperty(propertyName: "allowsNostr")]
        public bool AllowNostr { get; set; }

        [JsonProperty(propertyName: "nostrPubkey")]
        public string NostrPubKey { get; set; }
    }
    [JsonConverter(typeof(NParser))]
    public class LNPayEndpointResponse_Metadata
    {
        [NArrayElement(0)]
        public List<string> Key { get; set; }
        [NArrayElement(1)]
        public List<string> Data { get; set; }
    }
}
