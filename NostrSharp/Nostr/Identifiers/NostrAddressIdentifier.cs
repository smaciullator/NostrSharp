using NostrSharp.Extensions;
using NostrSharp.Nostr.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NostrSharp.Nostr.Identifiers
{
    public class NostrAddressIdentifier : NostrIdentifier
    {
        public NostrAddressIdentifier(string identifier, string? pubkey, string[]? relays, NKind? kind, byte[] data)
        {
            Identifier = identifier;
            Pubkey = pubkey;
            Kind = kind;
            Relays = relays ?? Array.Empty<string>();
            OriginalIdentifier = data.HexKeyToBech32String(Hrp);
        }

        public override string? OriginalIdentifier { get; protected set; }
        public override string Hrp => Bech32Identifiers.NAddr;
        public override string Special => Identifier;
        public string Identifier { get; init; }
        public string? Pubkey { get; init; }
        public string[] Relays { get; init; }
        public NKind? Kind { get; init; }

        public override string ToBech32()
        {
            List<(byte, byte[])> tlvData = new List<(byte, byte[])>
            {
                (SpecialKey, Identifier.UTF8AsByteArray()),
            };
            tlvData.AddRange(Relays.Select(relay => (RelayKey, Encoding.ASCII.GetBytes(relay))));
            if (!string.IsNullOrWhiteSpace(Pubkey))
                tlvData.Add((AuthorKey, Convert.FromHexString(Pubkey)));
            if (Kind != null)
                tlvData.Add((KindKey, WriteKind(Kind.Value)));
            byte[] tlv = NostrIdentifierParser.BuildTlv(tlvData.ToArray());
            return tlv.HexKeyToBech32String(Hrp) ?? string.Empty;
        }

        public static NostrAddressIdentifier Parse(byte[] data)
        {
            IReadOnlyCollection<KeyValuePair<byte, byte[]>> tlv = NostrIdentifierParser.ParseTlv(data);
            return new NostrAddressIdentifier(
                FindSpecialString(tlv),
                FindAuthor(tlv),
                FindRelays(tlv),
                FindKind(tlv),
                data
            );
        }
    }
}
