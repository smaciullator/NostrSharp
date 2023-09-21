using NostrSharp.Cryptography;
using NostrSharp.Nostr.Enums;
using System;
using System.Security.Cryptography;
using System.Text;

namespace NostrSharp.Extensions
{
    public static class StringExtensions
    {
        public static byte[] GetSha256(this string data)
        {
            byte[] sha256 = SHA256.HashData(data.UTF8AsByteArray());
            return sha256;
        }
        public static byte[] UTF8AsByteArray(this string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }


        public static byte[] HexToByteArray(this string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                char a = hex[i << 1];
                arr[i] = (byte)((hex[i << 1].GetHexValue() << 4) + hex[(i << 1) + 1].GetHexValue());
            }

            return arr;
        }


        public static string? HexKeyToBech32(this string? hexKey, string hrp)
        {
            if (string.IsNullOrWhiteSpace(hexKey))
                return hexKey;

            byte[] hexArray = hexKey.HexToByteArray();
            return hexArray.HexKeyToBech32String(hrp);
        }
        public static string? HexToNpubBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NPub);
        }
        public static string? HexToNsecBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NSec);
        }
        public static string? HexToNoteBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.Note);
        }
        public static string? HexToProfileBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NProfile);
        }
        public static string? HexToEventBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NEvent);
        }
        public static string? HexToRelayBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NRelay);
        }
        public static string? HexToAddressBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NAddr);
        }


        public static byte[]? Bech32ToHexBytes(this string? bech32, out string? hrp)
        {
            hrp = null;
            if (string.IsNullOrWhiteSpace(bech32))
                return Array.Empty<byte>();

            Bech32.Decode(bech32, out hrp, out byte[]? decoded);
            return decoded;
        }
        public static string? Bech32ToHexKey(this string? bech32, out string? hrp)
        {
            hrp = null;
            if (string.IsNullOrWhiteSpace(bech32))
                return bech32;

            Bech32.Decode(bech32, out hrp, out byte[]? decoded);
            string? hex = decoded?.ToHexString();
            return hex;
        }
    }
}
