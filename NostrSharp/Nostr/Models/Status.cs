using NostrSharp.Nostr.Enums;
using System;

namespace NostrSharp.Nostr.Models
{
    public class Status
    {
        public string Type { get; private set; }
        public string Content { get; set; }
        public string? Url { get; set; }
        public string? Profile { get; set; }
        public string? EventId { get; set; }
        public string? EventCoordinates { get; set; }
        public DateTime? Expiration { get; set; }


        public Status() { }
        private Status(NStatusType type, string content, string? url, string? profile, string? eventId, NEvent? ev, DateTime? expiration)
            : this(type, content, url, profile, eventId, ev?.GetCoordinates(), expiration) { }
        private Status(NStatusType type, string content, string? url, string? profile, string? eventId, string? eventCoordinates, DateTime? expiration)
        {
            Type = type.ToString();
            Content = content;
            Url = url;
            Profile = profile;
            EventId = eventId;
            EventCoordinates = eventCoordinates;
            Expiration = expiration;
        }


        public Status GeneralStatus(string content, string? url, string? profile, string? eventId, NEvent? ev, DateTime? expiration)
        {
            return GeneralStatus(content, url, profile, eventId, ev?.GetCoordinates(), expiration);
        }
        public Status GeneralStatus(string content, string? url, string? profile, string? eventId, string? eventCoordinates, DateTime? expiration)
        {
            return new(
                NStatusType.general,
                content,
                url,
                profile,
                eventId,
                eventCoordinates,
                expiration
            );
        }

        public Status MusicStatus(string content, string? url, string? profile, string? eventId, NEvent? ev, DateTime? expiration)
        {
            return MusicStatus(content, url, profile, eventId, ev?.GetCoordinates(), expiration);
        }
        public Status MusicStatus(string content, string? url, string? profile, string? eventId, string? eventCoordinates, DateTime? expiration)
        {
            return new(
                NStatusType.music,
                content,
                url,
                profile,
                eventId,
                eventCoordinates,
                expiration
            );
        }
    }
}
