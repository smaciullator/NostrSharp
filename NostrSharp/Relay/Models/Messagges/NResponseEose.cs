﻿using Newtonsoft.Json;
using NostrSharp.Json;

namespace NostrSharp.Relay.Models.Messagges
{
    [JsonConverter(typeof(NParser))]
    public class NResponseEose : NBaseMessage
    {
        [NArrayElement(1)]
        public string? SubscriptionId { get; set; }


        public NResponseEose() { }
    }
}
