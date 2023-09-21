using System;

namespace NostrSharp.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeStamp(this DateTime data)
        {
            return ((DateTimeOffset)data).ToUnixTimeSeconds();
        }
    }
}
