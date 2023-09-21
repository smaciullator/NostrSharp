using Newtonsoft.Json;

namespace NostrSharp.Relay.Models
{
    public class RelayPermissions
    {
        [JsonProperty("read", Order = 0)]
        public bool Read { get; set; }
        [JsonProperty("write", Order = 1)]
        public bool Write { get; set; }


        public RelayPermissions() { }
        public RelayPermissions(bool read, bool write)
        {
            Read = read;
            Write = write;
        }
    }
}
