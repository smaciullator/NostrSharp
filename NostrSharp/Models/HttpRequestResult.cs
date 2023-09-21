using System.Net;

namespace NostrSharp.Models
{
    public class HttpRequestResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Data { get; set; }
    }
}
