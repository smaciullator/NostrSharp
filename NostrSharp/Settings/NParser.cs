using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace NostrSharp.Settings
{
    public class NParser : JsonConverter
    {
        private static readonly ConcurrentDictionary<(MemberInfo, Type), Attribute?> CustomAttributesCache = new();
        private static readonly ConcurrentDictionary<(Type, Type), Attribute?> CustomAttributesTrypeCache = new();
        private static T? GetCachedAttribute<T>(MemberInfo mi) where T : Attribute => (T?)CustomAttributesCache.GetOrAdd((mi, typeof(T)), _ => mi.GetCustomAttribute(typeof(T)));
        private static T? GetCachedAttribute<T>(Type ty) where T : Attribute => (T?)CustomAttributesTrypeCache.GetOrAdd((ty, typeof(T)), _ => ty.GetCustomAttribute(typeof(T)));


        public override bool CanConvert(Type objectType)
        {
            return true;
        }
        public override bool CanRead => true;
        public override bool CanWrite => true;


        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JArray? arr = null;
            JObject? obj = null;
            try { arr = JArray.Load(reader); }
            catch (Exception ex)
            {
                string a = ex.Message;
                try { obj = JObject.Load(reader); }
                catch (Exception ex2)
                {
                    string b = ex2.Message;
                }
            }

            int index = -1;
            object? result = Activator.CreateInstance(objectType, true);

            if (IsList(objectType) && obj is not null && !objectType.GetProperties().Any(x => GetCachedAttribute<NArrayElementAttribute>(x) is not null))
            {
                IEnumerable? values = (IEnumerable?)obj;
                if (values is null)
                    return null;
                foreach (object? item in values)
                {
                    if (item is null)
                        continue;
                    JsonConverterAttribute? c = null;
                    if (objectType.GenericTypeArguments.Any())
                        c = GetCachedAttribute<JsonConverterAttribute>(objectType.GenericTypeArguments[0]);
                    if (c is not null)
                        return JsonSerializer.Create(SerializerCustomSettings.Settings).Deserialize(obj.CreateReader(), objectType.GenericTypeArguments[0]) ?? null;
                }
            }
            else if (IsList(objectType) && arr is not null)
            {
                Type t = objectType;
                if (!objectType.GenericTypeArguments.Any())
                    return null;
                t = objectType.GenericTypeArguments[0];


                var genericList = Activator.CreateInstance(typeof(List<>).MakeGenericType(t));
                int listIndex = -1;
                for (int i = 0; i < arr.Count; i++)
                {
                    JToken token = arr[i];
                    ((IList)genericList).Add(token.ToObject(t));
                }
                return genericList;
            }

            if (objectType.GetProperties().Any(x => GetCachedAttribute<NItemNameAttribute>(x) is not null || GetCachedAttribute<NItemValueAttribute>(x) is not null))
            {
                var genericList = Activator.CreateInstance(typeof(List<>).MakeGenericType(objectType));

                foreach (var item in obj)
                {
                    index++;
                    object? nameValueItem = Activator.CreateInstance(objectType, true);
                    foreach (PropertyInfo p in objectType.GetProperties().OrderBy(x => GetCachedAttribute<NArrayElementAttribute>(x)?.Index))
                    {
                        if (p is null
                            || p.PropertyType.IsNotPublic
                            || !p.CanWrite
                            || p.CustomAttributes.Any(x => x.AttributeType == typeof(JsonIgnoreAttribute)))
                            continue;
                        NItemNameAttribute? nItemName = GetCachedAttribute<NItemNameAttribute>(p);
                        NItemValueAttribute? nItemValue = GetCachedAttribute<NItemValueAttribute>(p);

                        if (nItemName is not null)
                            p.SetValue(nameValueItem, item.Key);
                        else if (nItemValue is not null)
                            p.SetValue(nameValueItem, item.Value?.ToObject(p.PropertyType));
                    }
                    ((IList)genericList).Add(nameValueItem);
                }

                return genericList;
            }

            foreach (PropertyInfo p in objectType.GetProperties().OrderBy(x => GetCachedAttribute<NArrayElementAttribute>(x)?.Index))
            {
                if (p is null
                    || p.PropertyType.IsNotPublic
                    || !p.CanWrite
                    || p.CustomAttributes.Any(x => x.AttributeType == typeof(JsonIgnoreAttribute)))
                    continue;

                object? value = null;

                if (arr is not null)
                {
                    index++;
                    JToken token;
                    try
                    {
                        if (arr.Count == index)
                            continue;
                        token = arr[index];
                        value = token.ToObject(p.PropertyType);
                    }
                    catch (Exception ex)
                    {
                        string a = ex.Message;
                    }
                }
                else if (obj is not null)
                {
                    string name = p.Name;
                    JsonPropertyAttribute? jsonPropAttribute = GetCachedAttribute<JsonPropertyAttribute>(p);
                    if (jsonPropAttribute is not null)
                        name = jsonPropAttribute.PropertyName ?? p.Name;


                    JToken token = obj[name];
                    if (token is not null)
                    {
                        if ((p.PropertyType == typeof(DateTime?) || p.PropertyType == typeof(DateTime)) && token.Type == JTokenType.Integer)
                            value = token.ToObject(p.PropertyType, JsonSerializer.Create(new() { Converters = { (JsonConverter?)Activator.CreateInstance(typeof(UnixDateTimeConverter)) ?? throw new InvalidOperationException("Cannot create JsonConverter") } }));
                        else
                            value = token.ToObject(p.PropertyType);
                    }
                }

                p.SetValue(result, value);
            }
            return result;
        }

        public override void WriteJson(JsonWriter writer, object? main, JsonSerializer serializer)
        {
            if (main == null)
                return;

            PropertyInfo[] properties = main.GetType().GetProperties();

            if (IsList(main.GetType()))
            {
                ParseArrayOrList(main, true, writer);
                return;
            }

            bool asNArrayAttributes = properties.Any(x => GetCachedAttribute<NArrayElementAttribute>(x) is not null) || IsList(main.GetType());

            if (main is not null)
            {
                if (asNArrayAttributes)
                    writer.WriteStartArray();
                else
                    writer.WriteStartObject();
            }

            foreach (PropertyInfo? p in properties.OrderBy(x => GetCachedAttribute<NArrayElementAttribute>(x)?.Index))
            {
                if (p is null
                    || p.PropertyType.IsNotPublic
                    || !p.CanWrite
                    || p.CustomAttributes.Any(x => x.AttributeType == typeof(JsonIgnoreAttribute)))
                    continue;

                bool isArrayOrList = p.PropertyType.IsArray || IsList(p.GetValue(main));
                object? value = null;
                if (isArrayOrList)
                    value = p.GetValue(main, null);
                else
                    value = p.GetValue(main);

                if (value is null)
                    continue;

                NArrayElementAttribute? nArrayAttr = GetCachedAttribute<NArrayElementAttribute>(p);
                // Serializzazione normale
                if (nArrayAttr is null)
                {
                    JsonPropertyAttribute? jsonPropAttribute = GetCachedAttribute<JsonPropertyAttribute>(p);
                    if (jsonPropAttribute is null)
                        writer.WritePropertyName(p.Name);
                    else
                        writer.WritePropertyName(jsonPropAttribute.PropertyName ?? p.Name);
                }

                // Nel caso di un array o lista devo serializzare iterando ogni elemento
                if (isArrayOrList)
                    ParseArrayOrList(value, nArrayAttr is null, writer);
                // Se invece si tratta di 
                else
                    ParseWriteElement(writer, value, p.PropertyType);
            }

            if (main is not null)
            {
                if (asNArrayAttributes)
                    writer.WriteEndArray();
                else
                    writer.WriteEndObject();
            }
        }


        private static void ParseArrayOrList(object? value, bool writeAsArray, JsonWriter writer)
        {
            IEnumerable? values = (IEnumerable?)value;
            if (values is null)
                return;
            if (writeAsArray)
                writer.WriteStartArray();
            foreach (object? item in values)
            {
                if (item is null)
                    continue;
                ParseWriteElement(writer, item, item.GetType());
            }
            if (writeAsArray)
                writer.WriteEndArray();
        }
        private static void ParseWriteElement(JsonWriter writer, object? value, Type valueType)
        {
            JsonConverterAttribute? c = GetCachedAttribute<JsonConverterAttribute>(valueType);

            // Se la proprietà ha un tag JsonConverter dichiarato nella classe
            if (c is not null)
            {
                JsonConverter? converter = (JsonConverter?)Activator.CreateInstance(c.ConverterType);
                if (converter is null)
                    throw new InvalidOperationException("Cannot create JsonConverter");
                writer.WriteRawValue(JsonConvert.SerializeObject(value, converter));
            }
            // Se la proprietà è un tipo generico, primitivo o numerico lo scrivo direttamente
            else if (IsSimple(valueType))
                writer.WriteValue(value);
            // Altrimenti i tipi complessi senza tag JsonConverter nella dichiarazione di classe vengono
            // serializzati secondo le configurazioni custom
            else
                JsonSerializer.Create(SerializerCustomSettings.Settings).Serialize(writer, value);
        }
        private static bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            if (type.IsArray)
                return false;
            return type.IsPrimitive
              || type.IsEnum
              || type == typeof(string)
              || type == typeof(decimal)
              || type == typeof(double)
              || type == typeof(float)
              || type == typeof(short)
              || type == typeof(int)
              || type == typeof(long);
        }
        private static bool IsList(object? o)
        {
            if (o == null)
                return false;
            return o is IList
                   && IsList(o.GetType());
        }
        private static bool IsList(Type type)
        {
            return type.IsGenericType
                   && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class NArrayElementAttribute : Attribute
    {
        public int Index { get; }

        public NArrayElementAttribute(int index)
        {
            Index = index;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class NItemNameAttribute : Attribute
    {
        public NItemNameAttribute() { }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class NItemValueAttribute : Attribute
    {
        public NItemValueAttribute() { }
    }
}
