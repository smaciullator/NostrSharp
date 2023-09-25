using NostrSharp.Json;
using System;
using System.Text.Json.Serialization;

namespace NostrSharp.Nostr.Models
{
    [JsonConverter(typeof(NParser))]
    public class Contact : IDisposable
    {
        [NArrayElement(0)]
        public string ContactPubKey { get; private set; }
        [NArrayElement(1)]
        public string? PreferredRelayUri { get; private set; }
        [NArrayElement(2)]
        public string? PetName { get; private set; }


        public Contact() { }
        public Contact(string contactPubKey) : this(contactPubKey, null, null) { }
        public Contact(string contactPubKey, string? preferredRelayUri) : this(contactPubKey, preferredRelayUri, null) { }
        public Contact(string contactPubKey, string? preferredRelayUri, string? petName)
        {
            ContactPubKey = contactPubKey;
            PreferredRelayUri = preferredRelayUri;
            PetName = petName;
        }


        public void Dispose()
        {
            ContactPubKey = null;
            PreferredRelayUri = null;
            PetName = null;
        }
    }
}
