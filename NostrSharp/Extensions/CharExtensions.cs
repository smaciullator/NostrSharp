namespace NostrSharp.Extensions
{
    public static class CharExtensions
    {
        public static int GetHexValue(this char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
