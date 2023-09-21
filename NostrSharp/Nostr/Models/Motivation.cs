using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models
{
    public class Motivation
    {
        [JsonProperty(propertyName: "reason")]
        public string Reason { get; set; }


        public Motivation() { }
        public Motivation(string reason)
        {
            Reason = reason;
        }
    }
}
