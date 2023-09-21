using Newtonsoft.Json;
using NostrSharp.Settings;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NResponseAuth : NBaseMessage
    {
        [NArrayElement(1)]
        public string? ChallengeString { get; set; }


        public NResponseAuth() { }
    }
}
