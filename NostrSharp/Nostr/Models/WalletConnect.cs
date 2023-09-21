using Newtonsoft.Json;
using NostrSharp.Keys;

namespace NostrSharp.Nostr.Models
{
    public class WalletConnect
    {
        [JsonProperty(propertyName: "pubkey")]
        public string WalletPubkey { get; set; }

        [JsonProperty(propertyName: "relay")]
        public string? RelayUrl { get; set; }

        [JsonProperty(propertyName: "secret")]
        public string? WalletSecret { get; set; }

        [JsonIgnore]
        public NSec? WalletNSec
        {
            get
            {
                if (string.IsNullOrEmpty(WalletSecret))
                    return null;
                NSec nsec = null;
                try { nsec = NSec.FromHex(WalletSecret); }
                catch
                {
                    try { nsec = NSec.FromBech32(WalletSecret); }
                    catch { }
                }
                return nsec;
            }
        }

        [JsonProperty("lud16")]
        public string? Lud16 { get; set; }
    }
}
