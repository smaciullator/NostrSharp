using NostrSharp.Extensions;
using NostrSharp.Nostr.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NostrSharp.Nostr.Identifiers
{
    public class NostrProfileIdentifier : NostrIdentifier
    {
        public NostrProfileIdentifier(string pubkey, string[]? relays, byte[] data)
        {
            Pubkey = pubkey;
            Relays = relays ?? Array.Empty<string>();
            OriginalIdentifier = data.HexKeyToBech32String(Hrp);
        }

        public override string? OriginalIdentifier { get; protected set; }
        public override string Hrp => Bech32Identifiers.NProfile;
        public override string Special => Pubkey;
        public string Pubkey { get; init; }
        public string[] Relays { get; init; }

        public override string ToBech32()
        {
            List<(byte, byte[])> tlvData = new List<(byte, byte[])>
            {
                (SpecialKey, Convert.FromHexString(Pubkey)),
            };
            tlvData.AddRange(Relays.Select(relay => (RelayKey, Encoding.ASCII.GetBytes(relay))));
            byte[] tlv = NostrIdentifierParser.BuildTlv(tlvData.ToArray());
            return tlv.HexKeyToBech32String(Hrp) ?? string.Empty;
        }

        public static NostrProfileIdentifier Parse(byte[] data)
        {
            IReadOnlyCollection<KeyValuePair<byte, byte[]>> tlv = NostrIdentifierParser.ParseTlv(data);
            return new NostrProfileIdentifier(
                FindSpecialHex(tlv),
                FindRelays(tlv),
                data
            );
        }
    }
}
