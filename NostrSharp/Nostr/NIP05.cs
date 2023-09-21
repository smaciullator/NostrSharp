using System.Collections.Generic;

namespace NostrSharp.Nostr
{
    public class NIP05
    {
        public string PubKey { get; set; } = null;
        public List<string>? PreferredRelays { get; set; }


        public NIP05() { }
        public NIP05(string pubKey, List<string>? preferredRelays)
        {
            PubKey = pubKey;
            PreferredRelays = preferredRelays;
        }
    }
}
