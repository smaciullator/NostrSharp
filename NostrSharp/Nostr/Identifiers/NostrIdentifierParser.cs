using NostrSharp.Extensions;
using NostrSharp.Nostr.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NostrSharp.Nostr.Identifiers
{
    public static class NostrIdentifierParser
    {
        public static NostrIdentifier? Parse(string? bech32)
        {
            byte[]? data = bech32.Bech32ToHexBytes(out string? hrp);
            if (data == null || !data.Any() || string.IsNullOrWhiteSpace(hrp))
                return null;

            return hrp switch
            {
                Bech32Identifiers.NProfile => NostrProfileIdentifier.Parse(data),
                Bech32Identifiers.NEvent => NostrEventIdentifier.Parse(data),
                Bech32Identifiers.NRelay => NostrRelayIdentifier.Parse(data),
                Bech32Identifiers.NAddr => NostrAddressIdentifier.Parse(data),
                _ => throw new InvalidOperationException(
                    $"Bech32 {hrp} identifier not yet supported, contact library maintainers")
            };
        }

        public static bool TryParse(string? bech32, out NostrIdentifier? identifier)
        {
            identifier = null;
            try
            {
                identifier = Parse(bech32);
                return true;
            }
            catch (Exception)
            {
                // ignore
            }

            return false;
        }

        internal static IReadOnlyCollection<KeyValuePair<byte, byte[]>> ParseTlv(byte[] tlvData)
        {
            List<KeyValuePair<byte, byte[]>> result = new List<KeyValuePair<byte, byte[]>>();
            int pos = 0;
            while (pos < tlvData.Length)
            {
                byte tag = tlvData[pos++];
                int length = tlvData[pos++];

                // handle extended length encoding
                if ((length & 0x80) != 0)
                {
                    int lengthBytes = length & 0x7F;
                    length = 0;
                    for (int i = 0; i < lengthBytes; i++)
                    {
                        length = (length << 8) + tlvData[pos++];
                    }
                }

                byte[] value = new byte[length];
                Array.Copy(tlvData, pos, value, 0, length);
                result.Add(new KeyValuePair<byte, byte[]>(tag, value));
                pos += length;
            }

            return result;
        }

        internal static byte[] BuildTlv((byte, byte[])[] tlvList)
        {
            List<byte> result = new List<byte>();

            foreach ((byte, byte[]) item in tlvList)
            {
                byte tag = item.Item1;
                byte[] value = item.Item2;
                int length = value.Length;

                // handle extended length encoding
                byte lengthBytes = length > 127 ? (byte)Math.Ceiling(length / 256.0) : (byte)0;
                if (lengthBytes > 0)
                {
                    length = (int)Math.Pow(256, lengthBytes) + length;
                }

                result.Add(tag);
                if (lengthBytes > 0)
                {
                    result.Add((byte)(0x80 | lengthBytes));
                    for (int i = lengthBytes - 1; i >= 0; i--)
                    {
                        result.Add((byte)(length / (int)Math.Pow(256, i)));
                    }
                }
                else
                {
                    result.Add((byte)length);
                }

                result.AddRange(value);
            }

            return result.ToArray();
        }
    }
}
