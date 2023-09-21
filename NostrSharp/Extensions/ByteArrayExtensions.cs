using NostrSharp.Cryptography;
using System;
using System.Text;

namespace NostrSharp.Extensions
{
    public static class ByteArrayExtensions
    {
        public static string ToUTF8String(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
        public static string ToHexString(this byte[] bytes)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();

            //StringBuilder builder = new StringBuilder();
            //foreach (byte b in bytes)
            //    builder.Append(b.ToHexString());

            //return builder.ToString();
        }
        public static string? HexKeyToBech32String(this byte[]? hexArray, string hrp)
        {
            if (hexArray == null)
                return null;

            string? npub = Bech32.Encode(hrp, hexArray);
            return npub;
        }
    }
}
