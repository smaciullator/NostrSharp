using Newtonsoft.Json;
using NostrSharp.Json;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NResponseNotice : NBaseMessage
    {
        [NArrayElement(1)]
        public string? Message { get; set; }


        public NResponseNotice() { }
    }
}
