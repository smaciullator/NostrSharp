using Newtonsoft.Json;
using NostrSharp.Json;
using NostrSharp.Nostr.Enums;
using System;
using System.Collections.Generic;

namespace NostrSharp.Nostr
{
    /// <summary>
    /// Usata solo in fase di calcolo dell'Id in un'istanza di tipo NEvent
    /// </summary>
    [JsonConverter(typeof(NEventSignParser))]
    public class NEventSign : IDisposable
    {
        public int Id { get; init; } = 0;
        public string PubKey { get; init; }

        public DateTime CreatedAt { get; init; }
        public int Kind { get; private set; }
        public List<NTag> Tags { get; init; }
        public string? Content { get; init; }


        public NEventSign() { }
        public NEventSign(string pubKey, DateTime createdAt, NKind kind, List<NTag> tags, string? content)
        {
            PubKey = pubKey;
            CreatedAt = createdAt;
            Kind = (int)kind;
            Tags = tags;
            Content = content;
        }


        public void Dispose()
        {
            if (Tags is not null)
            {
                foreach (NTag tag in Tags)
                    tag.Dispose();
                Tags.Clear();
            }
        }
    }
}
