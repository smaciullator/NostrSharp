using Newtonsoft.Json;
using NostrSharp.Json;
using NostrSharp.Keys;
using System;

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
        /// example: [[\"text/plain\",\"Pay to Wallet of Satoshi user: dynamicclick68\"],[\"text/identifier\",\"dynamicclick68@walletofsatoshi.com\"]]
        /// </summary>
        [JsonProperty(propertyName: "metadata")]
        public string? _metadata { get; set; }

        [JsonIgnore]
        public LNPayEndpointResponse_Metadata Metadata => string.IsNullOrEmpty(_metadata) ? new() : JsonConvert.DeserializeObject<LNPayEndpointResponse_Metadata>(_metadata, SerializerCustomSettings.Settings) ?? new();

        [JsonProperty(propertyName: "commentAllowed")]
        public string CommentAllowed { get; set; }

        [JsonProperty(propertyName: "tag")]
        public string Tag { get; set; }

        [JsonProperty(propertyName: "allowsNostr")]
        public bool AllowNostr { get; set; }

        [JsonProperty(propertyName: "nostrPubkey")]
        public string NostrPubKey { get; set; }
        [JsonIgnore]
        public bool IsNostrPubKeyValid
        {
            get
            {
                if (string.IsNullOrEmpty(NostrPubKey))
                    return false;
                try { NPub.FromHex(NostrPubKey); }
                catch { return false; }
                return true;
            }
        }
    }
}
