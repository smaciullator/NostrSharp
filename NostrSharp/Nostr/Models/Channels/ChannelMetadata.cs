using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models.Channels
{
    public class ChannelMetadata
    {
        [JsonProperty(propertyName: "name")]
        public string Name { get; set; }
        [JsonProperty(propertyName: "about")]
        public string About { get; set; }
        [JsonProperty(propertyName: "picture")]
        public string PictureUrl { get; set; }


        public ChannelMetadata() { }
        public ChannelMetadata(string name, string about, string pictureUrl)
        {
            Name = name;
            About = about;
            PictureUrl = pictureUrl;
        }
    }
}
