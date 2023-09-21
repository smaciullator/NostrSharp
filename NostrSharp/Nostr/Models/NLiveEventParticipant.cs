using NostrSharp.Nostr.Enums;

namespace NostrSharp.Nostr.Models
{
    public class NLiveEventParticipant
    {
        public string Pubkey { get; set; }
        public string RelayUri { get; set; }
        public NLiveEventRole Role { get; set; }
        public string StringifiedRole => Role.ToString();
        public string? Proof { get; set; }


        public NLiveEventParticipant() { }
        public NLiveEventParticipant(string pubkey, string relayUri, NLiveEventRole role) : this(pubkey, relayUri, role, null) { }
        /// <summary>
        /// The proof is a signed SHA256 of the complete a Tag of the event (kind:pubkey:dTag) by each p's private key, encoded in hex
        /// </summary>
        /// <param name="pubkey"></param>
        /// <param name="relayUri"></param>
        /// <param name="role"></param>
        /// <param name="proof"></param>
        public NLiveEventParticipant(string pubkey, string relayUri, NLiveEventRole role, string proof)
        {
            Pubkey = pubkey;
            RelayUri = relayUri;
            Role = role;
            Proof = proof;
        }
    }
}
