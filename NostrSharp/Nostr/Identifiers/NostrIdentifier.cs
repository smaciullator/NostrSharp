using NostrSharp.Extensions;
using NostrSharp.Nostr.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace NostrSharp.Nostr.Identifiers
{
    /// <summary>
    /// Shareable identifier with extra metadata.
    /// When sharing a profile or an event, an app may decide to include relay information
    /// and other metadata such that other apps can locate and display these entities more easily.
    /// </summary>
    public abstract class NostrIdentifier
    {
        protected const byte SpecialKey = 0x00;
        protected const byte RelayKey = 0x01;
        protected const byte AuthorKey = 0x02;
        protected const byte KindKey = 0x03;

        /// <summary>
        /// The original string
        /// </summary>
        public abstract string? OriginalIdentifier { get; protected set; }

        /// <summary>
        /// Bech32 prefix (nprofile, nevent, nrelay, naddr, etc.)
        /// </summary>
        public abstract string Hrp { get; }

        /// <summary>
        /// Primary value, depends on the bech32 prefix (hrp)
        /// </summary>
        public abstract string Special { get; }

        /// <summary>
        /// Convert back to bech32 format
        /// </summary>
        public abstract string ToBech32();

        protected static string FindSpecialHex(IReadOnlyCollection<KeyValuePair<byte, byte[]>> tlv)
        {
            return tlv
                .First(pair => pair.Key == SpecialKey).Value
                .AsSpan()
                .ToHexString();
        }

        protected static string FindSpecialString(IReadOnlyCollection<KeyValuePair<byte, byte[]>> tlv)
        {
            return tlv.First(pair => pair.Key == SpecialKey).Value.ToUTF8String();
        }

        protected static string? FindAuthor(IReadOnlyCollection<KeyValuePair<byte, byte[]>> tlv)
        {
            return ValueOrNull(tlv
                .FirstOrDefault(pair => pair.Key == AuthorKey).Value
                .AsSpan()
                .ToHexString());
        }

        protected static string[] FindRelays(IReadOnlyCollection<KeyValuePair<byte, byte[]>> tlv)
        {
            return tlv
                .Where(pair => pair.Key == RelayKey)
                .Select(pair => Encoding.ASCII.GetString(pair.Value))
                .ToArray();
        }

        protected static NKind? FindKind(IReadOnlyCollection<KeyValuePair<byte, byte[]>> tlv)
        {
            byte[] kind = tlv
                .FirstOrDefault(pair => pair.Key == KindKey).Value;
            if (kind == null)
                return null;
            uint kindInt = BitConverter.ToUInt32(kind.Reverse().ToArray());
            return (NKind)kindInt;
        }

        protected static byte[] WriteKind(NKind kind)
        {
            return BitConverter.GetBytes((uint)kind).Reverse().ToArray();
        }

        private static string? ValueOrNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
