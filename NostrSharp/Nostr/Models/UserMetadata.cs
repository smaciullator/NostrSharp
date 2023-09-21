using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models
{
    public class UserMetadata
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("username")]
        public string? Username { get; set; }
        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }
        [JsonProperty("display_name")]
        public string? Display_Name { get; set; }
        [JsonProperty("about")]
        public string? About { get; set; }
        [JsonProperty("picture")]
        public string? Picture { get; set; }
        [JsonProperty("banner")]
        public string? Banner { get; set; }
        [JsonProperty("website")]
        public string? Website { get; set; }
        [JsonProperty("nip05")]
        public string? Nip05 { get; set; }
        [JsonProperty("lud16")]
        public string? Lud16 { get; set; }
        [JsonProperty("lud06")]
        public string? Lud06 { get; set; }
        public string PubKey { get; set; }


        public UserMetadata() { }
        public UserMetadata(string? name, string? username, string? displayName, string? display_Name, string? about, string? picture, string? banner, string? website, string? nip05, string? lud16, string? lud06)
        {
            Name = name;
            Username = username;
            DisplayName = displayName;
            Display_Name = display_Name;
            About = about;
            Picture = picture;
            Banner = banner;
            Website = website;
            Nip05 = nip05;
            Lud16 = lud16;
            Lud06 = lud06;
        }
    }
}
