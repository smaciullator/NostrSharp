using Newtonsoft.Json;
using NostrSharp.Settings;
using System;

namespace NostrSharp.Nostr
{
    [JsonConverter(typeof(NParser))]
    public class NTag : IDisposable
    {
        [NArrayElement(0)]
        public string Key { get; set; }
        [NArrayElement(1)]
        public string? Value { get; set; }
        [NArrayElement(2)]
        public string? SecondValue { get; set; }
        [NArrayElement(3)]
        public string? ThirdValue { get; set; }
        [NArrayElement(4)]
        public string? FourthValue { get; set; }


        public NTag() { }
        public NTag(string? key)
        {
            Key = key;
        }
        public NTag(string key, string value)
        {
            Key = key;
            Value = value;
        }
        public NTag(string key, string value, string? secondValue)
        {
            Key = key;
            Value = value;
            SecondValue = secondValue;
        }
        public NTag(string key, string value, string? secondValue, string? thirdValue)
        {
            Key = key;
            Value = value;
            SecondValue = secondValue;
            ThirdValue = thirdValue;
        }
        public NTag(string key, string value, string? secondValue, string? thirdValue, string? fourthValue)
        {
            Key = key;
            Value = value;
            SecondValue = secondValue;
            ThirdValue = thirdValue;
            FourthValue = fourthValue;
        }
        public NTag(string key, string value, object[]? additionalData)
        {
            Key = key;
            Value = value;

            if (additionalData is not null)
                for (int i = 0; i < additionalData.Length; i++)
                    if (i == 0)
                        SecondValue = additionalData[i].ToString();
                    else if (i == 1)
                        ThirdValue = additionalData[i].ToString();
                    else if (i == 2)
                        FourthValue = additionalData[i].ToString();
        }
        public NTag(string key, object[] values)
        {
            Key = key;
            Value = values[0].ToString();

            for (int i = 1; i < values.Length; i++)
                if (i == 1)
                    SecondValue = values[i].ToString();
                else if (i == 2)
                    ThirdValue = values[i].ToString();
                else if (i == 3)
                    FourthValue = values[i].ToString();
        }


        public void Dispose()
        {
            Key = null;
            Value = null;
            SecondValue = null;
            ThirdValue = null;
            FourthValue = null;
        }
    }
}
