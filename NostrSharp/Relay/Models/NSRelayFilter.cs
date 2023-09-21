using Newtonsoft.Json;
using NostrSharp.Nostr.Enums;
using System;

namespace NostrSharp.Relay.Models
{
    public class NSRelayFilter
    {
        /// <summary>
        /// Lista di id degli eventi da filtrare
        /// </summary>
        [JsonProperty("ids")]
        public string[]? EventIds { get; set; }
        /// <summary>
        /// Lista di NPubs in lowercase per filtrare gli eventi in base all'autore
        /// </summary>
        [JsonProperty("authors")]
        public string[]? Authors { get; set; }
        /// <summary>
        /// Lista numerica di tutti i Kinds da filtrare
        /// </summary>
        [JsonProperty("kinds")]
        public NKind[]? Kinds { get; set; }

        /// <summary>
        /// Lista di id degli eventi referenziati all'interno di un tag "e"
        /// </summary>
        [JsonProperty("#e")]
        public string[]? E { get; set; }

        /// <summary>
        /// Lista di id degli eventi referenziati all'interno di un tag "p"
        /// </summary>
        [JsonProperty("#p")]
        public string[]? P { get; set; }

        /// <summary>
        /// Lista di id degli eventi referenziati all'interno di un tag "a"
        /// </summary>
        [JsonProperty("#a")]
        public string[]? A { get; set; }

        /// <summary>
        /// Filtra gli eventi il cui "created_at" è successivo valore indicato
        /// </summary>
        [JsonProperty("since")]
        public DateTime? Since { get; set; }
        /// <summary>
        /// Filtra gli eventi il cui "created_at" è precedente al valore indicato
        /// </summary>
        [JsonProperty("until")]
        public DateTime? Until { get; set; }

        /// <summary>
        /// Il numero massimo di eventi che il relay dovrebbe restituire nella query iniziale
        /// </summary>
        [JsonProperty("limit")]
        public int? Limit { get; set; }

        [JsonProperty("search")]
        public string? Search { get; set; }


        public NSRelayFilter() { }
        public NSRelayFilter(string? eventId, string? author) : this(eventId is null ? null : new string[1] { eventId }, author is null ? null : new string[1] { author }, null, null, null, null, null, null, null, null) { }
        public NSRelayFilter(string[]? eventIds, string[]? authors) : this(eventIds, authors, null, null, null, null, null, null, null, null) { }
        public NSRelayFilter(string[]? e, string[]? p, string[]? a) : this(null, null, null, e, p, a, null, null, null, null) { }
        public NSRelayFilter(string? author, NKind? kind) : this(null, author is null ? null : new string[1] { author }, kind is null ? null : new NKind[1] { (NKind)kind }, null, null, null, null, null, null, null) { }
        public NSRelayFilter(string? author, NKind[] kinds) : this(null, author is null ? null : new string[1] { author }, kinds, null, null, null, null, null, null, null) { }
        public NSRelayFilter(string[]? authors, NKind[] kinds) : this(null, authors, kinds, null, null, null, null, null, null, null) { }
        public NSRelayFilter(string[]? authors, NKind? kind, DateTime? since, DateTime? until) : this(null, authors, kind is null ? null : new NKind[1] { (NKind)kind }, null, null, null, since, until, null, null) { }
        public NSRelayFilter(string[]? authors, NKind[] kinds, DateTime? since, DateTime? until) : this(null, authors, kinds, null, null, null, since, until, null, null) { }
        public NSRelayFilter(NKind? kind, string? eventId) : this(eventId is null ? null : new string[1] { eventId }, null, kind is null ? null : new NKind[1] { (NKind)kind }, null, null, null, null, null, null, null) { }
        public NSRelayFilter(NKind? kind, string[]? eventIds) : this(eventIds, null, kind is null ? null : new NKind[1] { (NKind)kind }, null, null, null, null, null, null, null) { }
        public NSRelayFilter(NKind? kind) : this(null, null, kind is null ? null : new NKind[1] { (NKind)kind }, null, null, null, null, null, null, null) { }
        public NSRelayFilter(NKind[] kinds) : this(null, null, kinds, null, null, null, null, null, null, null) { }
        public NSRelayFilter(NKind[] kinds, string? eventId) : this(eventId is null ? null : new string[1] { eventId }, null, kinds, null, null, null, null, null, null, null) { }
        public NSRelayFilter(NKind[] kinds, string[]? eventIds) : this(eventIds, null, kinds, null, null, null, null, null, null, null) { }
        public NSRelayFilter(DateTime? since, DateTime? until) : this(null, null, null, null, null, null, since, until, null, null) { }
        public NSRelayFilter(int limit) : this(null, null, null, null, null, null, null, null, limit, null) { }
        public NSRelayFilter(string search) : this(null, null, null, null, null, null, null, null, null, search) { }
        public NSRelayFilter(string[]? eventIds, string[]? authors, NKind[]? kinds, string[]? e, string[]? p, string[]? a, DateTime? since, DateTime? until, int? limit, string? search)
        {
            EventIds = eventIds;
            Authors = authors;
            Kinds = kinds;
            E = e;
            P = p;
            A = a;
            Since = since;
            Until = until;
            Limit = limit;
            Search = search;
        }


        public static NSRelayFilter FromEvent(string eventId)
        {
            return FromEvents(new string[1] { eventId });
        }
        public static NSRelayFilter FromEvents(string[] eventIds)
        {
            return new NSRelayFilter(eventIds, null, null, null, null, null, null, null, null, null);
        }
        public static NSRelayFilter FromEventTag(string eventId)
        {
            return FromEventTag(new string[1] { eventId });
        }
        public static NSRelayFilter FromEventTag(string[] eventIds)
        {
            return new NSRelayFilter(null, null, null, eventIds, null, null, null, null, null, null);
        }
    }
}
