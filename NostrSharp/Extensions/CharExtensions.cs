namespace NostrSharp.Extensions
{
    public static class CharExtensions
    {
        /// <summary>
        /// Return the hex value representation of the given char
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static int GetHexValue(this char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
