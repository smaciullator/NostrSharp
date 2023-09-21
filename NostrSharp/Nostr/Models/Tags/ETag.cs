namespace NostrSharp.Nostr.Models.Tags
{
    public class ETag
    {
        public string EventId { get; set; }
        public string? PreferredRelay { get; set; } = null;


        public ETag() { }
        public ETag(string eventId, string? preferredRelay = null)
        {
            EventId = eventId;
            PreferredRelay = preferredRelay;
        }
    }
}
