using System;

namespace NostrSharp.Nostr.Models.Tags
{
    public class RelayTag
    {
        public string RelayUri { get; set; }
        public string? CustomMarker { get; set; } = null;


        public RelayTag() { }
        public RelayTag(Uri relayUri, string? customMarker = null) : this(relayUri.ToString(), customMarker) { }
        public RelayTag(string relayUri, string? customMarker = null)
        {
            RelayUri = relayUri;
            CustomMarker = customMarker;
        }
    }
}
