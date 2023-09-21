namespace NostrSharp.Nostr.Models.Tags
{
    public class ZapTag
    {
        public string HexPubKey { get; set; }
        public string MetadataRelayUrl { get; set; }
        public decimal? Weight { get; set; } = null;


        public ZapTag() { }
        public ZapTag(string hexPubKey, string metadataRelayUrl, decimal? weight)
        {
            HexPubKey = hexPubKey;
            MetadataRelayUrl = metadataRelayUrl;
            Weight = weight;
        }
    }
}
