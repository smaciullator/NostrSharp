namespace NostrSharp.Extensions
{
    public static class ByteExtensions
    {
        public static string ToHexString(this byte b)
        {
            return b.ToString("x2");
        }
    }
}
