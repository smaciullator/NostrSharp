using NostrSharp.Nostr.Enums;

namespace NostrSharp.Nostr.Models.Tags
{
    public class ATag
    {
        public string Coordinates { get; set; }
        public string? PreferredRelay { get; set; } = null;
        public string? CustomMarker { get; private set; } = null;


        public ATag() { }
        public ATag(NEvent originalEvent, string? preferredRelay = null, HandlerPlatform? platform = null) : this(originalEvent.GetCoordinates(), preferredRelay, platform) { }
        public ATag(string coordinates, string? preferredRelay = null, HandlerPlatform? platform = null)
        {
            Coordinates = coordinates;
            PreferredRelay = preferredRelay;
            CustomMarker = platform.ToString();
        }
        public ATag(NEvent originalEvent, string? preferredRelay = null, NMarkers? customMarker = null) : this(originalEvent.GetCoordinates(), preferredRelay, customMarker) { }
        public ATag(string coordinates, string? preferredRelay = null, NMarkers? customMarker = null)
        {
            Coordinates = coordinates;
            PreferredRelay = preferredRelay;
            CustomMarker = customMarker.ToString();
        }
    }
}
