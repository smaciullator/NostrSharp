using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace NostrSharp.Settings
{
    public static class SerializerCustomSettings
    {
        public static JsonSerializerSettings Settings => new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            Converters = new List<JsonConverter>
            {
                new UnixDateTimeConverter(),
                new NParser()
            },
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        public static JsonSerializerSettings EventSignSettings => new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            Converters = new List<JsonConverter>
            {
                new UnixDateTimeConverter(),
                new NEventSignParser()
            },
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    }
}
