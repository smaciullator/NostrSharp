using NostrSharp.Nostr.Enums;
using NostrSharp.Settings;
using System.Text.Json.Serialization;

namespace NostrSharp.Nostr.Models
{
    [JsonConverter(typeof(NParser))]
    public class NProxy
    {
        [NArrayElement(0)]
        public string UniqueID { get; set; }
        [NArrayElement(1)]
        public string Type { get; set; }


        public NProxy() { }
        public NProxy(string uniqueID, NProxyTypes type)
        {
            UniqueID = uniqueID;
            Type = type.ToString();
        }
    }
}
