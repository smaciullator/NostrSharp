using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models
{
    public class WalletRequest
    {
        [JsonProperty(propertyName: "method")]
        public string Method { get; set; }

        [JsonProperty(propertyName: "params")]
        public WalletRequestParams Parameters { get; set; }


        public WalletRequest() { }
        public WalletRequest(string method, WalletRequestParams parameters)
        {
            Method = method;
            Parameters = parameters;
        }


        public static WalletRequest CreatePayInvoiceRequest(string invoiceLN)
        {
            return new()
            {
                Method = "pay_invoice",
                Parameters = new()
                {
                    Invoice = invoiceLN
                }
            };
        }
    }
    public class WalletRequestParams
    {
        [JsonProperty(propertyName: "invoice")]
        public string Invoice { get; set; }
    }
}
