using Newtonsoft.Json;

namespace NostrSharp.Models.LN
{
    public class LNZapRequestResponse
    {
        [JsonProperty(propertyName: "pr")]
        public string? Invoice { get; set; }

        [JsonProperty(propertyName: "status")]
        public string? Status { get; set; }

        [JsonProperty(propertyName: "reason")]
        public string? Reason { get; set; }
    }
}
