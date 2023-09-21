using Newtonsoft.Json;
using System.Collections.Generic;

namespace NostrSharp.Nostr.Models
{
    public class Label
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public LabelMetadata? Metadata { get; set; }
        public List<NTag> References { get; set; }


        public Label() { }
        public Label(string _namespace, string name) : this(_namespace, name, null, new(), null) { }
        private Label(string _namespace, string name, LabelMetadata? metadata, List<NTag> references, string? description)
        {
            Namespace = _namespace;
            Name = name;
            Metadata = metadata;
            References = references;
            Description = description;
        }


        /// <summary>
        /// Accetta un Dictionary con le pubkeys come Key, e opzionalmente il preferred relay come Value
        /// </summary>
        /// <param name="_namespace"></param>
        /// <param name="name"></param>
        /// <param name="metadata"></param>
        /// <param name="userReferences"></param>
        /// <returns></returns>
        public static Label PubkeysLabel(string _namespace, string name, LabelMetadata? metadata, Dictionary<string, string?> userReferences, string? description = null)
        {
            List<NTag> tags = new();
            foreach (KeyValuePair<string, string?> user in userReferences)
                tags.Add(new("p", user.Key, user.Value));

            return new(
                _namespace,
                name,
                metadata,
                tags,
                description
            );
        }
        public static Label RelaysLabel(string _namespace, string name, LabelMetadata? metadata, List<string> relayUrls, string? description = null)
        {
            List<NTag> tags = new();
            foreach (string relayUrl in relayUrls)
                tags.Add(new("r", relayUrl));

            return new(
                _namespace,
                name,
                metadata,
                tags,
                description
            );
        }
        /// <summary>
        /// Accetta un Dictionary con gli id degli eventi come Key, e opzionalmente il preferred relay come Value
        /// </summary>
        /// <param name="_namespace"></param>
        /// <param name="name"></param>
        /// <param name="metadata"></param>
        /// <param name="eventReferences"></param>
        /// <returns></returns>
        public static Label EventsLabel(string _namespace, string name, LabelMetadata? metadata, Dictionary<string, string?> eventReferences, string? description = null)
        {
            List<NTag> tags = new();
            foreach (KeyValuePair<string, string?> ev in eventReferences)
                tags.Add(new("e", ev.Key, ev.Value));

            return new(
                _namespace,
                name,
                metadata,
                tags,
                description
            );
        }
        /// <summary>
        /// Accetta un Dictionary con il riferimento al replaceable event nel formato <Kind>:<32 byte of npub>:<"d" tag value> come Key, e a
        /// seguire il preferred relay come primo elemento opzionale dei Values. A seguire, dentro Values, è possibile aggiungere altri parametri
        /// opzionali del tag
        /// </summary>
        /// <param name="_namespace"></param>
        /// <param name="name"></param>
        /// <param name="metadata"></param>
        /// <param name="eventReferences"></param>
        /// <returns></returns>
        public static Label ReplaceableEventsLabel(string _namespace, string name, LabelMetadata? metadata, Dictionary<string, object[]?> eventReferences, string? description = null)
        {
            List<NTag> tags = new();
            foreach (KeyValuePair<string, object[]?> ev in eventReferences)
                tags.Add(new("a", ev.Key, ev.Value));

            return new(
                _namespace,
                name,
                metadata,
                tags,
                description
            );
        }
    }
    public class LabelMetadata
    {
        [JsonProperty(propertyName: "quality")]
        public float? Quality { get; set; }
        [JsonProperty(propertyName: "confidence")]
        public float? Confidence { get; set; }
        [JsonProperty(propertyName: "context")]
        public object[]? Context { get; set; }
    }
}
