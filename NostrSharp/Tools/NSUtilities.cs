using BTCPayServer.Lightning;
using Newtonsoft.Json;
using NostrSharp.Cryptography;
using NostrSharp.Extensions;
using NostrSharp.Keys;
using NostrSharp.Models;
using NostrSharp.Models.LN;
using NostrSharp.Nostr;
using NostrSharp.Nostr.Enums;
using NostrSharp.Nostr.Identifiers;
using NostrSharp.Nostr.Models;
using NostrSharp.Relay.Models;
using NostrSharp.Settings;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NostrSharp.Tools
{
    public static class NSUtilities
    {
        public static async Task<NIP05?> CheckNIP05(string nPubHex, string nip05)
        {
            if (!nip05.Contains("@"))
                return null;
            string[] parts = nip05.Split('@');
            if (parts.Length != 2)
                return null;

            string domain = parts[1];
            string name = parts[0];

            string nip05Url = $"https://{domain}/.well-known/nostr.json?name={name}";

            HttpRequestResult result = await HttpRequestUtilites.Get(nip05Url);
            if (string.IsNullOrEmpty(result.Data))
                return null;

            NIP05Response? response = JsonConvert.DeserializeObject<NIP05Response>(result.Data, SerializerCustomSettings.Settings);
            if (response is null || response.Names is null || response.Names.Count == 0)
                return null;

            bool nameFound = response.Names.ContainsKey(nPubHex);

            List<string> preferredRelays = new();
            // Opzionalmente, se trovo i releay preferiti per questa chiave, li restituisco
            if (nameFound && response.Relays is not null && response.Relays.Count > 0)
                preferredRelays = response.Relays.Where(x => x.Key == nPubHex).SelectMany(x => x.Value.ToList()).ToList(); //.ToDictionary(x => x.Key, x => x.Value);
            return new(nPubHex, preferredRelays);
        }

        public static async Task<RelayNIP11Metadata?> GetNIP11RelayMetadata(Uri relayUri)
        {
            try
            {
                NameValueCollection customHeaders = new NameValueCollection();
                customHeaders.Add("Accept", "application/nostr+json");
                HttpRequestResult result = await HttpRequestUtilites.Get(relayUri.ToString().Replace("wss://", "https://"), customHeaders);
                return string.IsNullOrEmpty(result.Data) ? null : JsonConvert.DeserializeObject<RelayNIP11Metadata>(result.Data, SerializerCustomSettings.Settings);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static bool ParseNIP19Identifier(string? bech32, out NostrIdentifier? identifier)
        {
            identifier = null;
            try
            {
                byte[]? data = bech32.Bech32ToHexBytes(out string? hrp);
                if (data == null || !data.Any() || string.IsNullOrWhiteSpace(hrp))
                    return false;

                identifier = hrp switch
                {
                    Bech32Identifiers.NProfile => NostrProfileIdentifier.Parse(data),
                    Bech32Identifiers.NEvent => NostrEventIdentifier.Parse(data),
                    Bech32Identifiers.NRelay => NostrRelayIdentifier.Parse(data),
                    Bech32Identifiers.NAddr => NostrAddressIdentifier.Parse(data),
                    _ => throw new InvalidOperationException(
                        $"Bech32 {hrp} identifier not yet supported, contact library maintainers")
                };
                return true;
            }
            catch { }
            return false;
        }
        public static bool ParseNIP19ProfileIdentifier(string? bech32, out NostrProfileIdentifier? profile)
        {
            profile = null;
            if (!ParseNIP19Identifier(bech32, out NostrIdentifier? identifier))
                return false;

            switch (identifier)
            {
                case NostrProfileIdentifier parsedProfile:
                    profile = parsedProfile;
                    break;
                case NostrRelayIdentifier parsedRelay:
                case NostrEventIdentifier parsedEvent:
                case NostrAddressIdentifier parsedAddress:
                    break;
            }
            return profile is not null;
        }
        public static bool ParseNIP19RelayIdentifier(string? bech32, out NostrRelayIdentifier? relay)
        {
            relay = null;
            if (!ParseNIP19Identifier(bech32, out NostrIdentifier? identifier))
                return false;

            switch (identifier)
            {
                case NostrRelayIdentifier parsedRelay:
                    relay = parsedRelay;
                    break;
                case NostrProfileIdentifier parsedProfile:
                case NostrEventIdentifier parsedEvent:
                case NostrAddressIdentifier parsedAddress:
                    break;
            }
            return relay is not null;
        }
        public static bool ParseNIP19EventIdentifier(string? bech32, out NostrEventIdentifier? ev)
        {
            ev = null;
            if (!ParseNIP19Identifier(bech32, out NostrIdentifier? identifier))
                return false;

            switch (identifier)
            {
                case NostrEventIdentifier parsedEvent:
                    ev = parsedEvent;
                    break;
                case NostrRelayIdentifier parsedRelay:
                case NostrProfileIdentifier parsedProfile:
                case NostrAddressIdentifier parsedAddress:
                    break;
            }
            return ev is not null;
        }
        public static bool ParseNIP19AddressIdentifier(string? bech32, out NostrAddressIdentifier? address)
        {
            address = null;
            if (!ParseNIP19Identifier(bech32, out NostrIdentifier? identifier))
                return false;

            switch (identifier)
            {
                case NostrAddressIdentifier parsedAddress:
                    address = parsedAddress;
                    break;
                case NostrEventIdentifier parsedEvent:
                case NostrRelayIdentifier parsedRelay:
                case NostrProfileIdentifier parsedProfile:
                    break;
            }
            return address is not null;
        }


        public static WalletConnect? ReadNIP47WalletConnectUri(Uri walletConnectUri)
        {
            // Esempio:
            // nostr+walletconnect:b889ff5b1513b641e2a139f661a661364979c5beee91842f8f0ef42ab558e9d4?relay=wss%3A%2F%2Frelay.damus.io&secret=71a8c14c1407c113601079c4302dab36460f0ccd0ad506f1f2dc73b5100e4f3c

            if (walletConnectUri.Scheme != "nostrwalletconnect" && walletConnectUri.Scheme != "nostr+walletconnect")
                return null;

            string pubkey = walletConnectUri.Host;
            if (string.IsNullOrEmpty(pubkey))
                return null;

            WalletConnect wc = new();
            wc.WalletPubkey = pubkey;

            NameValueCollection parameters = HttpUtility.ParseQueryString(walletConnectUri.Query);
            wc.RelayUrl = parameters.Get("relay");
            wc.WalletSecret = parameters.Get("secret");
            wc.Lud16 = parameters.Get("lud16");
            return wc;
        }
        public static string GenerateNIP46NostrConnectLink(NPub npub, Uri preferredRelayUri, NostrConnectMetadata metadata)
        {
            return $"nostrconnect://{npub.Hex}?relay=\"{HttpUtility.UrlEncode(preferredRelayUri.ToString())}\"&metadata={HttpUtility.UrlEncode(JsonConvert.SerializeObject(metadata, SerializerCustomSettings.Settings))}";
        }


        public static string? ParsePayEndpoitFromLNURLorADDRESS(string lnUrlAddress)
        {
            // lud06
            if (lnUrlAddress.ToLowerInvariant().StartsWith("lnurl"))
            {
                Bech32.Decode(lnUrlAddress, out _, out byte[]? data);
                if (data is null)
                    return null;
                return data.ToUTF8String();
            }

            string[] parts = lnUrlAddress.Split("@");

            // Se è nel formato lnaddress example@walletofsatoshi.com
            if (parts.Length == 2)
                return $"https://{parts[1]}/.well-known/lnurlp/{parts[0]}";

            return null;
        }
        public static async Task<LNPayEndpointResponse?> FetchLNPayEndpoint(string payEndpoint)
        {
            try
            {
                NameValueCollection customHeaders = new NameValueCollection();
                customHeaders.Add("Accept", "application/nostr+json");
                HttpRequestResult result = await HttpRequestUtilites.Get(payEndpoint, customHeaders);
                return string.IsNullOrEmpty(result.Data) ? null : JsonConvert.DeserializeObject<LNPayEndpointResponse>(result.Data, SerializerCustomSettings.Settings);
            }
            catch
            {
                return null;
            }
        }
        public static async Task<LNZapRequestResponse?> FetchLNZapResponse(string callback, string ev, decimal satoshisAmount, string lnUrl)
        {
            try
            {
                string url = $"{callback}?amount={LightMoney.FromUnit(satoshisAmount, LightMoneyUnit.Satoshi).MilliSatoshi.ToString()}&nostr={HttpUtility.UrlEncode(ev)}&lnurl={lnUrl}";

                NameValueCollection customHeaders = new NameValueCollection();
                customHeaders.Add("Accept", "application/nostr+json");
                HttpRequestResult result = await HttpRequestUtilites.Get(url, customHeaders);
                return string.IsNullOrEmpty(result.Data) ? null : JsonConvert.DeserializeObject<LNZapRequestResponse>(result.Data, SerializerCustomSettings.Settings);
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// NIP98: HTTP Auth
        /// This one return the base64 encoded event as a Authorization header value
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/98.md
        /// </summary>
        /// <param name="nSec"></param>
        /// <param name="absoluteUrl"></param>
        /// <param name="method"></param>
        /// <param name="serializedRequestBody"></param>
        /// <returns></returns>
        public static string? GenerateNostrHeaderFromHttpAuthEvent(NSec nSec, string absoluteUrl, string method, string? serializedRequestBody = null)
        {
            NEvent? ev = NSEventMaker.HttpAuth(absoluteUrl, method, serializedRequestBody);
            if (ev is null || !ev.Sign(nSec))
                return null;
            return $"Nostr {Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ev, SerializerCustomSettings.Settings)))}";
        }
        /// <summary>
        /// NIP98: HTTP Auth
        /// This one return the base64 encoded event as a Authorization header value
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/98.md
        /// </summary>
        /// <param name="unsignedHttpAuthEvent">MUST NOT BE SIGNED</param>
        /// <param name="nSec"></param>
        /// <returns></returns>
        public static string? GenerateNostrHeaderFromHttpAuthEvent(NEvent unsignedHttpAuthEvent, NSec nSec)
        {
            if (unsignedHttpAuthEvent.Kind != NKind.HttpAuth || !unsignedHttpAuthEvent.Sign(nSec))
                return null;
            return $"Nostr {Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(unsignedHttpAuthEvent, SerializerCustomSettings.Settings)))}";
        }
    }
}
