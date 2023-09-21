using Newtonsoft.Json;

namespace NostrSharp.Nostr.Models
{
    public class WalletResponse
    {
        [JsonProperty(propertyName: "result_type")]
        public string ResultType { get; set; }

        [JsonProperty(propertyName: "error")]
        public WalletRequestError? Error { get; set; }

        [JsonProperty(propertyName: "result")]
        public WalletRequestResult? Result { get; set; }
        [JsonIgnore]
        public bool Success => Result is not null;
    }
    public class WalletRequestError
    {
        private const string RATE_LIMITED = "RATE_LIMITED";
        private const string NOT_IMPLEMENTED = "NOT_IMPLEMENTED";
        private const string INSUFFICIENT_BALANCE = "INSUFFICIENT_BALANCE";
        private const string QUOTA_EXCEEDED = "QUOTA_EXCEEDED";
        private const string RESTRICTED = "RESTRICTED";
        private const string UNAUTHORIZED = "UNAUTHORIZED";
        private const string INTERNAL = "INTERNAL";
        private const string OTHER = "OTHER";


        [JsonProperty(propertyName: "code")]
        public string? Code { get; set; }

        [JsonProperty(propertyName: "message")]
        public string? Message { get; set; }


        [JsonIgnore]
        public bool RateLimited => Code is not null && Code == RATE_LIMITED;
        [JsonIgnore]
        public bool NotImplemented => Code is not null && Code == NOT_IMPLEMENTED;
        [JsonIgnore]
        public bool InsufficientBalance => Code is not null && Code == INSUFFICIENT_BALANCE;
        [JsonIgnore]
        public bool QuotaExceeded => Code is not null && Code == QUOTA_EXCEEDED;
        [JsonIgnore]
        public bool Restricted => Code is not null && Code == RESTRICTED;
        [JsonIgnore]
        public bool Unauthorized => Code is not null && Code == UNAUTHORIZED;
        [JsonIgnore]
        public bool InternalError => Code is not null && Code == INTERNAL;
        [JsonIgnore]
        public bool Other => Code is not null && Code == OTHER;
    }
    public class WalletRequestResult
    {
        [JsonProperty(propertyName: "preimage")]
        public string? Preimage { get; set; }
    }
}
