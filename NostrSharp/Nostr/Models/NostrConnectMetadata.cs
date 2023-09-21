using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models
{
    public class NostrConnectMetadata
    {
        [JsonProperty(propertyName: "name")]
        public string AppName { get; set; }

        [JsonProperty(propertyName: "url")]
        public string? Url { get; set; }

        [JsonProperty(propertyName: "description")]
        public string? AppDescription { get; set; }

        [JsonProperty(propertyName: "icons")]
        public string[]? AppIconsUrl { get; set; }


        public NostrConnectMetadata() { }
        public NostrConnectMetadata(string appName, string? url, string? appDescription, string[]? appIconsUrl)
        {
            AppName = appName;
            Url = url;
            AppDescription = appDescription;
            AppIconsUrl = appIconsUrl;
        }
    }
}
