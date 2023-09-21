using NostrSharp.Models;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NostrSharp.Tools
{
    internal static class HttpRequestUtilites
    {
        private static HttpClient _client;
        public static JsonSerializerOptions _options;

        static HttpRequestUtilites()
        {
            _client = new HttpClient();
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IgnoreReadOnlyProperties = true,
                MaxDepth = 50
            };
        }

        internal static async Task<HttpRequestResult> Get(string url, NameValueCollection? additionalHeaders = null, bool bypassCertificateValidation = false, int timeout = 100000)
        {
            HttpRequestResult result = new() { StatusCode = HttpStatusCode.InternalServerError };

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

                if (additionalHeaders is not null)
                    foreach (string key in additionalHeaders)
                        request.Headers.Add(key, additionalHeaders[key]);

                HttpResponseMessage response = await _client.SendAsync(request);
                result.StatusCode = response.StatusCode;


                if (result.StatusCode == HttpStatusCode.OK)
                    result.Data = await response.Content.ReadAsStringAsync();
                else
                    result.ErrorMessage = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }
            return result;
        }
    }
}
