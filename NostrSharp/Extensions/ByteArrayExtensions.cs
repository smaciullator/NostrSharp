using NostrSharp.Cryptography;
using System;
using System.Text;

namespace NostrSharp.Extensions
{
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Return an UTF-8 encoded string from the given byte array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToUTF8String(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
        /// <summary>
        /// Return an Hex string from the given byte array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ToHexString(this byte[] bytes)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        /// <summary>
        /// Return a Bech32 string from the given byte array with the specified HRP
        /// </summary>
        /// <param name="hexArray"></param>
        /// <param name="hrp"></param>
        /// <returns></returns>
        public static string? HexKeyToBech32String(this byte[]? hexArray, string hrp)
        {
            if (hexArray == null)
                return null;

            return Bech32.Encode(hrp, hexArray);
        }
    }
}
