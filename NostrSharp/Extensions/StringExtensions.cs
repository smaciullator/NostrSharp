using NostrSharp.Cryptography;
using NostrSharp.Nostr.Enums;
using System;
using System.Security.Cryptography;
using System.Text;

namespace NostrSharp.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Return the SHA256 hashed representation of the given string as a byte array
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] GetSha256(this string data)
        {
            byte[] sha256 = SHA256.HashData(data.UTF8AsByteArray());
            return sha256;
        }
        /// <summary>
        /// Return a byte array from the given UTF-8 encoded string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] UTF8AsByteArray(this string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }


        /// <summary>
        /// Return the byte array representation of the given Hex string
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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


        /// <summary>
        /// Convert the given Hex string into the corresponding Bech32 string, in addition with the given HRP
        /// </summary>
        /// <param name="hexKey"></param>
        /// <param name="hrp"></param>
        /// <returns></returns>
        public static string? HexKeyToBech32(this string? hexKey, string hrp)
        {
            if (string.IsNullOrWhiteSpace(hexKey))
                return hexKey;

            byte[] hexArray = hexKey.HexToByteArray();
            return hexArray.HexKeyToBech32String(hrp);
        }


        /// <summary>
        /// Convert the given Hex string into the corresponding Bech32 string with the right HRP for NPub ("npub")
        /// </summary>
        /// <param name="hexKey"></param>
        /// <returns></returns>
        public static string? HexToNpubBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NPub);
        }
        /// <summary>
        /// Convert the given Hex string into the corresponding Bech32 string with the right HRP for NSec ("nsec")
        /// </summary>
        /// <param name="hexKey"></param>
        /// <returns></returns>
        public static string? HexToNsecBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NSec);
        }
        /// <summary>
        /// Convert the given Hex string into the corresponding Bech32 string with the right HRP for a Note ("note")
        /// </summary>
        /// <param name="hexKey"></param>
        /// <returns></returns>
        public static string? HexToNoteBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.Note);
        }
        /// <summary>
        /// Convert the given Hex string into the corresponding Bech32 string with the right HRP for a User Profile ("nprofile")
        /// </summary>
        /// <param name="hexKey"></param>
        /// <returns></returns>
        public static string? HexToProfileBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NProfile);
        }
        /// <summary>
        /// Convert the given Hex string into the corresponding Bech32 string with the right HRP for an event ("nevent")
        /// </summary>
        /// <param name="hexKey"></param>
        /// <returns></returns>
        public static string? HexToEventBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NEvent);
        }
        /// <summary>
        /// Convert the given Hex string into the corresponding Bech32 string with the right HRP for a relay ("nrelay")
        /// </summary>
        /// <param name="hexKey"></param>
        /// <returns></returns>
        public static string? HexToRelayBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NRelay);
        }
        /// <summary>
        /// Convert the given Hex string into the corresponding Bech32 string with the right HRP for an event address ("naddr")
        /// </summary>
        /// <param name="hexKey"></param>
        /// <returns></returns>
        public static string? HexToAddressBech32(this string? hexKey)
        {
            return hexKey.HexKeyToBech32(Bech32Identifiers.NAddr);
        }


        /// <summary>
        /// Convert a Bech32 string into it's byte array representation, and also output the initial HRP
        /// </summary>
        /// <param name="bech32"></param>
        /// <param name="hrp"></param>
        /// <returns></returns>
        public static byte[]? Bech32ToHexBytes(this string? bech32, out string? hrp)
        {
            hrp = null;
            if (string.IsNullOrWhiteSpace(bech32))
                return Array.Empty<byte>();

            Bech32.Decode(bech32, out hrp, out byte[]? decoded);
            return decoded;
        }
        /// <summary>
        /// Convert a Bech32 string into it's Hex string representation, and also output the initial HRP
        /// </summary>
        /// <param name="bech32"></param>
        /// <param name="hrp"></param>
        /// <returns></returns>
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
