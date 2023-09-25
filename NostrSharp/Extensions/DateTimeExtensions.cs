using System;

namespace NostrSharp.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Convert the given DateTime instance into it's UNIX seconds representation
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static long ToUnixTimeStamp(this DateTime data)
        {
            return ((DateTimeOffset)data).ToUnixTimeSeconds();
        }
    }
}
