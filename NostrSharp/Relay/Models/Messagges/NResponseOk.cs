using Newtonsoft.Json;
using NostrSharp.Json;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NResponseOk : NBaseMessage
    {
        [NArrayElement(1)]
        public string? EventId { get; set; }
        [NArrayElement(2)]
        public bool Accepted { get; set; }
        [NArrayElement(3)]
        public string? Message { get; set; }


        public NResponseOk() { }
    }
}
