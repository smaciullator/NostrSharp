using Newtonsoft.Json;
using System;

namespace NostrSharp.Relay.Models
{
    public class NSRelayConfig
    {
        [JsonIgnore]
        public Uri Uri { get; set; }
        public string UriString
        {
            get => Uri is null ? "" : Uri.ToString();
            set => Uri = new Uri(value);
        }
        public TimeSpan? ReconnectTimeout { get; set; } = null;
        public TimeSpan? ErrorReconnectTimeout { get; set; } = null;
        public RelayPermissions? RelayPermissions { get; set; } = null;


        public NSRelayConfig() { }
        public NSRelayConfig(string uri) : this(uri, null) { }
        public NSRelayConfig(string uri, RelayPermissions permissions) : this(new Uri(!uri.StartsWith("wss://") ? $"wss://{uri.Replace("https://", "")}" : uri.Replace("https://", "")), null, null, permissions) { }
        public NSRelayConfig(Uri uri) : this(uri, null) { }
        public NSRelayConfig(Uri uri, RelayPermissions? permissions) : this(uri, null, null, permissions) { }
        public NSRelayConfig(Uri uri, TimeSpan? reconnectTimeout, TimeSpan? errorReconnectTimeout, RelayPermissions? permissions)
        {
            Uri = uri;
            ReconnectTimeout = reconnectTimeout;
            ErrorReconnectTimeout = errorReconnectTimeout;
            RelayPermissions = permissions;
        }
    }
}
