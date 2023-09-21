using NostrSharp.Extensions;
using NostrSharp.Nostr.Enums;
using System.Collections.Generic;

namespace NostrSharp.Nostr.Identifiers
{
    public class NostrRelayIdentifier : NostrIdentifier
    {
        public NostrRelayIdentifier(string relay, byte[] data)
        {
            Relay = relay;
            OriginalIdentifier = data.HexKeyToBech32String(Hrp);
        }

        public override string? OriginalIdentifier { get; protected set; }
        public override string Hrp => Bech32Identifiers.NRelay;
        public override string Special => Relay;
        public string Relay { get; init; }

        public override string ToBech32()
        {
            List<(byte, byte[])> tlvData = new List<(byte, byte[])>
            {
                (SpecialKey, Relay.UTF8AsByteArray()),
            };
            byte[] tlv = NostrIdentifierParser.BuildTlv(tlvData.ToArray());
            return tlv.HexKeyToBech32String(Hrp) ?? string.Empty;
        }

        public static NostrRelayIdentifier Parse(byte[] data)
        {
            IReadOnlyCollection<KeyValuePair<byte, byte[]>> tlv = NostrIdentifierParser.ParseTlv(data);
            return new NostrRelayIdentifier(
                FindSpecialString(tlv),
                data
            );
        }
    }
}
