using Newtonsoft.Json;
using NostrSharp.Json;

namespace NostrSharp.Relay.Models
{
    [JsonConverter(typeof(NParser))]
    public class RelayInfo
    {
        [NItemNameAttribute]
        public string RelayUri { get; set; }
        [NItemValueAttribute]
        public RelayPermissions RelayPermissions { get; set; }


        public RelayInfo() { }
        public RelayInfo(string relayUri, bool read = true, bool write = true) : this(relayUri, new(read, write)) { }
        public RelayInfo(string relayUri, RelayPermissions relayPermissions)
        {
            RelayUri = relayUri;
            RelayPermissions = relayPermissions;
        }
    }
}
