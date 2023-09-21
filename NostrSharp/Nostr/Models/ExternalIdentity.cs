namespace NostrSharp.Nostr.Models
{
    public class ExternalIdentity
    {
        public string PlatformName { get; private set; }
        public string PlatformUsername { get; private set; }
        public string Parameter => $"{PlatformName}:{PlatformUsername}";
        public string Proof { get; private set; }


        public ExternalIdentity() { }
        public ExternalIdentity(string platformName, string platformUsername, string proof)
        {
            PlatformName = platformName;
            PlatformUsername = platformUsername.ToLowerInvariant();
            Proof = proof;
        }
    }
}
