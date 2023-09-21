using NostrSharp.Extensions;
using NostrSharp.Nostr.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NostrSharp.Nostr.Identifiers
{
    public class NostrEventIdentifier : NostrIdentifier
    {
        public NostrEventIdentifier(string eventId, string? pubkey, string[]? relays, NKind? kind, byte[] data)
        {
            EventId = eventId;
            Pubkey = pubkey;
            Kind = kind;
            Relays = relays ?? Array.Empty<string>();
            OriginalIdentifier = data.HexKeyToBech32String(Hrp);
        }

        public override string? OriginalIdentifier { get; protected set; }
        public override string Hrp => Bech32Identifiers.NEvent;
        public override string Special => EventId;
        public string EventId { get; init; }
        public string? Pubkey { get; init; }
        public string[] Relays { get; init; }
        public NKind? Kind { get; init; }

        public override string ToBech32()
        {
            List<(byte, byte[])> tlvData = new List<(byte, byte[])>
            {
                (SpecialKey, Convert.FromHexString(EventId))
            };
            if (!string.IsNullOrWhiteSpace(Pubkey))
                tlvData.Add((AuthorKey, Convert.FromHexString(Pubkey)));
            tlvData.AddRange(Relays.Select(relay => (RelayKey, Encoding.ASCII.GetBytes(relay))));
            if (Kind != null)
                tlvData.Add((KindKey, WriteKind(Kind.Value)));
            byte[] tlv = NostrIdentifierParser.BuildTlv(tlvData.ToArray());
            return tlv.HexKeyToBech32String(Hrp) ?? string.Empty;
        }

        public static NostrEventIdentifier Parse(byte[] data)
        {
            IReadOnlyCollection<KeyValuePair<byte, byte[]>> tlv = NostrIdentifierParser.ParseTlv(data);
            return new NostrEventIdentifier(
                FindSpecialHex(tlv),
                FindAuthor(tlv),
                FindRelays(tlv),
                FindKind(tlv),
                data
            );
        }
    }
}
