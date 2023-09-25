using Newtonsoft.Json;
using NostrSharp.Json;
using System.Collections.Generic;

namespace NostrSharp.Models.LN
{

    [JsonConverter(typeof(NParser))]
    public class LNPayEndpointResponse_Metadata
    {
        [NArrayElement(0)]
        public List<string> Key { get; set; }
        [NArrayElement(1)]
        public List<string> Data { get; set; }
    }
}
