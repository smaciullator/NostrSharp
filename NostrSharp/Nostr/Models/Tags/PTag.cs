namespace NostrSharp.Nostr.Models.Tags
{
    public class PTag
    {
        public string PubKeyHex { get; set; }
        public string? PreferredRelay { get; set; } = null;
        public string? Role { get; set; } = null;
        public string? Proof { get; set; } = null;


        public PTag() { }
        public PTag(string pubKeyHex, string? preferredRelay = null, string? role = null, string? proof = null)
        {
            PubKeyHex = pubKeyHex;
            PreferredRelay = preferredRelay;
            Role = role;
            Proof = proof;
        }
    }
}
