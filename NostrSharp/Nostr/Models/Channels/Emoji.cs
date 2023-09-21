namespace NostrSharp.Nostr.Models.Channels
{
    public class Emoji
    {
        public string Name { get; set; }
        public string? UnemojifiedName => string.IsNullOrEmpty(Name) ? null : Name.Replace(":", "");
        public string? EmojifiedName => string.IsNullOrEmpty(Name) ? null : $":{UnemojifiedName}:";
        public string? Url { get; set; }
        public bool IsCustom { get; set; }


        public Emoji() { }
        public Emoji(string name) : this(name, null, false) { }
        public Emoji(string name, string? url) : this(name, url, true) { }
        public Emoji(string name, string? url, bool isCustom)
        {
            Name = name;
            Url = url;
            IsCustom = isCustom;
        }
    }
}
