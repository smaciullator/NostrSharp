using System;
using System.Text;

namespace NostrSharp.Extensions
{
    public static class SpanByteExtensions
    {
        public static string ToHexString(this Span<byte> bytes)
        {
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
                builder.Append(b.ToHexString());

            return builder.ToString();
        }
    }
}
