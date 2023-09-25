using Newtonsoft.Json;
using NostrSharp.Nostr;
using System;
using System.Linq;
using System.Reflection;

namespace NostrSharp.Json
{
    public class NEventSignParser : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(NEventSign);
        }
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object? main, JsonSerializer serializer)
        {
            if (main == null)
                return;

            writer.WriteStartArray();

            foreach (PropertyInfo? p in main.GetType().GetProperties())
            {
                if (p is null
                    || p.PropertyType.IsNotPublic
                    || !p.CanWrite
                    || p.CustomAttributes.Any(x => x.AttributeType == typeof(JsonIgnoreAttribute)))
                    continue;

                if (p.Name == "Id" || p.Name == "PubKey" || p.Name == "Kind" || p.Name == "Content")
                    writer.WriteValue(p.GetValue(main));
                else if (p.Name == "CreatedAt")
                    JsonSerializer.Create(SerializerCustomSettings.Settings).Serialize(writer, p.GetValue(main));
                else if (p.Name == "Tags")
                    JsonSerializer.Create(SerializerCustomSettings.Settings).Serialize(writer, p.GetValue(main, null));
            }

            writer.WriteEndArray();
        }
    }
}
