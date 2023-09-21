using NostrSharp.Nostr;
using NostrSharp.Nostr.Enums;
using NostrSharp.Nostr.Identifiers;
using System;
using System.Collections.Generic;

namespace NostrSharp.Tools
{
    public static class EventContentParser
    {
        public static List<NostrIdentifier> ParseEventContent(NEvent ev)
        {
            if (ev is null || string.IsNullOrEmpty(ev.Content))
                return new();

            List<NostrIdentifier> identifiers = new();
            foreach (string rawChunk in ev.Content.Split("nostr:", StringSplitOptions.RemoveEmptyEntries))
            {
                if (rawChunk.Contains(Bech32Identifiers.NProfile)
                        || rawChunk.Contains(Bech32Identifiers.NEvent)
                        || rawChunk.Contains(Bech32Identifiers.NRelay)
                        || rawChunk.Contains(Bech32Identifiers.NAddr))
                    foreach (string chunks in rawChunk.Split(" ", StringSplitOptions.RemoveEmptyEntries))
                    {
                        NostrIdentifier? identifier = null;
                        if (chunks.StartsWith(Bech32Identifiers.NProfile)
                            || chunks.StartsWith(Bech32Identifiers.NEvent)
                            || chunks.StartsWith(Bech32Identifiers.NRelay)
                            || chunks.StartsWith(Bech32Identifiers.NAddr))
                            NostrIdentifierParser.TryParse(chunks, out identifier);
                        if (identifier is not null)
                            identifiers.Add(identifier);
                    }
            }
            return identifiers;
        }
    }
}
