using Newtonsoft.Json;
using NostrSharp.Cryptography;
using NostrSharp.Extensions;
using NostrSharp.Json;
using NostrSharp.Keys;
using NostrSharp.Models.LN;
using NostrSharp.Nostr;
using NostrSharp.Nostr.Enums;
using NostrSharp.Nostr.Models;
using NostrSharp.Nostr.Models.Tags;
using NostrSharp.Relay;
using NostrSharp.Relay.Models;
using NostrSharp.Relay.Models.Messagges;
using NostrSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NostrSharp
{
    public class NSMain : IDisposable
    {
        #region Events
        public delegate void EventHandler();
        public delegate void EventHandler<T1>(T1 p1);
        public delegate void EventHandler<T1, T2>(T1 p1, T2 p2);
        public delegate void EventHandler<T1, T2, T3>(T1 p1, T2 p2, T3 p3);


        public event EventHandler<Uri> OnInitialConnectionEstablished;
        public event EventHandler<Uri, string> OnConnectionClosed;

        public event EventHandler<Uri, NEvent> OnEvent;
        public event EventHandler<Uri, RelayNIP11Metadata> OnRelayMetadata;
        public event EventHandler<Uri, string?> OnAuthResponse;
        public event EventHandler<Uri, CountResult> OnCount;
        public event EventHandler<Uri, UserMetadata?, NEvent> OnMetadata;
        public event EventHandler<Uri, NEvent> OnShortTextNote;
        public event EventHandler<Uri, NEvent> OnReserved;
        public event EventHandler<Uri, NEvent, List<RelayInfo>> OnContacts;
        public event EventHandler<Uri, NEvent> OnEncryptedDm;
        public event EventHandler<Uri, NEvent> OnReaction;
        public event EventHandler<Uri, NEvent> OnGenericRepost;
        public event EventHandler<Uri, NEvent> OnLongFormContent;


        public event EventHandler<Uri, string[]> OnWalletInfoReceived;
        public event EventHandler<Uri, WalletResponse> OnWalletResponseReceived;
        public event EventHandler<Uri, NEvent> OnZap;


        public event EventHandler<Uri, string, string> OnEventRefused;
        public event EventHandler<Uri, string, string> OnEventApproved;
        public event EventHandler<Uri, string> OnError;
        #endregion


        public NPub? NPub { get; set; } = default;
        public NSec? NSec { get; set; } = default;

        public bool Initialized => NPub is not null || NSec is not null;
        public bool CanRead => Initialized;
        public bool CanWrite => NSec is not null;


        public NSMultiRelay Relays { get; private set; } = new();


        private List<NSRelayConfig> UserRelaysConfig => Relays is null ? new() : Relays.Relays.Select(x => x.Configurations).ToList();
        public WalletConnect? WCParams { get; private set; } = default;
        private Func<byte[], byte[], byte[], Task<string?>> _overrideDecryptionMethod { get; set; }


        public NSMain()
        {
            AttachEvents();
        }


        /// <summary>
        /// Initialize the instance with a key. You can pass both NPub or NSec in Hex or Bech32 format.
        /// For those platforms that do not support AES-ECB decryption you can override the decryption part
        /// by passing in a Func that has:
        ///     - first input parameter is the key
        ///     - second input parameter is the iv
        ///     - third input parameter is the encrypted string
        ///     - output parameter is in the plain text decrypted string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="overrideDecryptionMethod"></param>
        public bool Init(string key, Func<byte[], byte[], byte[], Task<string?>>? overrideDecryptionMethod = null)
        {
            try { Init(NPub.FromBech32(key), overrideDecryptionMethod); }
            catch
            {
                try { Init(NPub.FromHex(key), overrideDecryptionMethod); }
                catch
                {
                    try { Init(NSec.FromBech32(key), overrideDecryptionMethod); }
                    catch
                    {
                        try { Init(NSec.FromHex(key), overrideDecryptionMethod); }
                        catch { }
                    }
                }
            }
            return CanRead;
        }
        /// <summary>
        /// Initialize the instance with an npub and an nsec. You can pass string both in Hex or Bech32 format.
        /// For those platforms that do not support AES-ECB decryption you can override the decryption part
        /// by passing in a Func that has:
        ///     - first input parameter is the key
        ///     - second input parameter is the iv
        ///     - third input parameter is the encrypted string
        ///     - output parameter is in the plain text decrypted string
        /// </summary>
        /// <param name="npub"></param>
        /// <param name="nsec"></param>
        /// <param name="overrideDecryptionMethod"></param>
        public bool Init(string? npub, string? nsec, Func<byte[], byte[], byte[], Task<string?>>? overrideDecryptionMethod = null)
        {
            NPub? NPub = null;
            if (!string.IsNullOrEmpty(npub))
                try { NPub = NPub.FromBech32(npub); }
                catch
                {
                    try { NPub = NPub.FromHex(npub); }
                    catch { }
                }

            NSec? NSec = null;
            if (!string.IsNullOrEmpty(nsec))
                try { NSec = NSec.FromBech32(nsec); }
                catch
                {
                    try { NSec = NSec.FromHex(nsec); }
                    catch { }
                }

            return Init(NPub, NSec, overrideDecryptionMethod);
        }
        /// <summary>
        /// Initialize the instance with just an NPub, resulting in a READ-ONLY instance.
        /// For those platforms that do not support AES-ECB decryption you can override the decryption part
        /// by passing in a Func that has:
        ///     - first input parameter is the key
        ///     - second input parameter is the iv
        ///     - third input parameter is the encrypted string
        ///     - output parameter is in the plain text decrypted string
        /// </summary>
        /// <param name="nPub"></param>
        /// <param name="overrideDecryptionMethod"></param>
        public bool Init(NPub? nPub, Func<byte[], byte[], byte[], Task<string?>>? overrideDecryptionMethod = null)
        {
            NPub = nPub;
            NSec = null;
            if (overrideDecryptionMethod is not null)
                _overrideDecryptionMethod = overrideDecryptionMethod;
            return CanRead;
        }
        /// <summary>
        /// Initialize the instance with an NSec, and then derive it's NPub.
        /// For those platforms that do not support AES-ECB decryption you can override the decryption part
        /// by passing in a Func that has:
        ///     - first input parameter is the key
        ///     - second input parameter is the iv
        ///     - third input parameter is the encrypted string
        ///     - output parameter is in the plain text decrypted string
        /// </summary>
        /// <param name="nSec"></param>
        /// <param name="overrideDecryptionMethod"></param>
        public bool Init(NSec? nSec, Func<byte[], byte[], byte[], Task<string?>>? overrideDecryptionMethod = null)
        {
            NPub = nSec is not null ? nSec.DerivePublicKey() : null;
            NSec = nSec;
            if (overrideDecryptionMethod is not null)
                _overrideDecryptionMethod = overrideDecryptionMethod;
            return CanWrite;
        }
        /// <summary>
        /// Initialize the instance with a given NSec and a given NPub.
        /// For those platforms that do not support AES-ECB decryption you can override the decryption part
        /// by passing in a Func that has:
        ///     - first input parameter is the key
        ///     - second input parameter is the iv
        ///     - third input parameter is the encrypted string
        ///     - output parameter is in the plain text decrypted string
        /// </summary>
        /// <param name="nPub"></param>
        /// <param name="nSec"></param>
        /// <param name="overrideDecryptionMethod"></param>
        public bool Init(NPub? nPub, NSec? nSec, Func<byte[], byte[], byte[], Task<string?>>? overrideDecryptionMethod = null)
        {
            NPub = nPub;
            NSec = nSec;
            if (overrideDecryptionMethod is not null)
                _overrideDecryptionMethod = overrideDecryptionMethod;
            return CanRead && CanWrite;
        }


        public bool SetWalletConnectInfo(Uri walletConnectUri)
        {
            WCParams = NSUtilities.ReadNIP47WalletConnectUri(walletConnectUri);
            if (WCParams is null || string.IsNullOrEmpty(WCParams.RelayUrl) || WCParams.WalletNSec is null)
                return false;
            return true;
        }
        public void SetWalletConnectInfo(WalletConnect wc)
        {
            WCParams = wc;
        }
        public void SetWalletConnectInfo(string walletPubKey, string relayUri, string secret, string? lud16 = null)
        {
            WCParams = new()
            {
                WalletPubkey = walletPubKey,
                RelayUrl = relayUri,
                WalletSecret = secret,
                Lud16 = lud16
            };
        }


        private bool TryGetNPub(out NPub? nPub)
        {
            nPub = null;
            if (!Initialized)
                return false;
            if (NPub is not null)
                nPub = NPub;
            else if (NSec is not null)
                nPub = NSec.DerivePublicKey();
            return nPub is not null;
        }
        private bool TryGetNSec(out NSec? nSec)
        {
            nSec = null;
            if (!CanWrite)
                return false;
            nSec = NSec;
            return nSec is not null;
        }


        public async Task<List<Uri>> ConnectRelays(List<NSRelayConfig> relays)
        {
            List<Uri> nonRunningRelays = new();
            await Parallel.ForEachAsync(relays, async (relay, token) =>
            {
                if (!await ConnectRelay(relay))
                    nonRunningRelays.Add(relay.Uri);
            });
            return nonRunningRelays;
        }
        public async Task<bool> ConnectRelay(NSRelayConfig relay)
        {
            if (Relays.RunningRelays.Any(x => x.Configurations.Uri == relay.Uri))
                return true;

            Relays.AddRelay(relay);
            return await Relays.Connect(relay.Uri);
        }
        public async Task ConnectRelaysAsync(List<NSRelayConfig> relays)
        {
            Parallel.ForEachAsync(relays, async (relay, token) =>
            {
                await ConnectRelay(relay);
            });
        }

        public async Task<List<Uri>> DisconnectRelays(CancellationToken? token = null)
        {
            await Relays.SendClose(new NRequestClose(""), token);
            return await Relays.DisconnectAll();
        }
        public async Task<bool> DisconnectRelay(Uri relayUri)
        {
            await Relays.SendClose(relayUri, new NRequestClose(""));
            return await Relays.Disconnect(relayUri);
        }

        public async Task<List<Uri>> ReconnectRelays(CancellationToken? token = null)
        {
            List<NSRelayConfig> relaysToReconnect = new List<Uri>(Relays.RelaysUri).Select(x => new NSRelayConfig(x)).ToList();
            await DisconnectRelays(token);
            return await ConnectRelays(relaysToReconnect);
        }
        public async Task<bool> ReconnectRelay(Uri relayUri)
        {
            await DisconnectRelay(relayUri);
            return await ConnectRelay(new(relayUri));
        }


        public bool IsRelayRunning(Uri relayUri)
        {
            return Relays.RunningRelaysUri.Any(x => x == relayUri);
        }
        public bool IsRelayNotRunning(Uri relayUri)
        {
            return Relays.NonRunningRelaysUri.Any(x => x == relayUri);
        }
        public RelayPermissions? GetRelayPermissions(Uri relayUri)
        {
            NSRelayConfig? config = UserRelaysConfig.FirstOrDefault(x => x.Uri == relayUri);
            if (config is null)
                return null;
            return config.RelayPermissions;
        }


        public async Task<bool> SendAuthentication(Uri relayUri, string challengeString, CancellationToken? token = null)
        {
            if (!TryGetNSec(out NSec? nSec) || nSec is null)
                return false;

            NEvent authEvent = NSEventMaker.Authentication(relayUri, challengeString);
            if (!authEvent.Sign(nSec))
                return false;
            return await Relays.SendAuthentication(relayUri, new NRequestAuth(authEvent), token);
        }

        public async Task<List<Uri>> SendEvent(NEvent ev, CancellationToken? token = null)
        {
            List<Uri> errors = new();
            foreach (NSRelay relay in Relays.RunningRelays)
                if (!await SendEvent(relay.Configurations.Uri, ev, token))
                    errors.Add(relay.Configurations.Uri);
            return errors;
        }
        public async Task<bool> SendEvent(Uri relayUri, NEvent ev, CancellationToken? token = null)
        {
            if (!TryGetNSec(out NSec? nSec) || nSec is null)
                return false;
            if (!ev.Signed && !ev.Sign(nSec))
                return false;

            RelayPermissions? permissions = GetRelayPermissions(relayUri);
            if (permissions is not null && !permissions.Write)
                return false;

            return await Relays.SendEvent(relayUri, new NRequestEvent(ev), token);
        }

        public async Task<List<Uri>> SendFilter(NSRelayFilter filters, CancellationToken? token = null)
        {
            return await Relays.SendFilter(new NRequestReq("", filters), token);
        }
        public async Task<bool> SendFilter(Uri relayUri, NSRelayFilter filters, CancellationToken? token = null)
        {
            return await Relays.SendFilter(relayUri, new NRequestReq("", filters), token);
        }

        public async Task<List<Uri>> SendCount(NSRelayFilter filters, CancellationToken? token = null)
        {
            return await Relays.SendCount(new NRequestCount("", filters), token);
        }
        public async Task<bool> SendCount(Uri relayUri, NSRelayFilter filters, CancellationToken? token = null)
        {
            return await Relays.SendCount(relayUri, new NRequestCount("", filters), token);
        }

        public async Task<List<Uri>> SendClose(CancellationToken? token = null)
        {
            return await Relays.SendClose(new NRequestClose(""), token);
        }
        public async Task<bool> SendClose(Uri relayUri, CancellationToken? token = null)
        {
            return await Relays.SendClose(relayUri, new NRequestClose(""), token);
        }


        #region Metadata + Contacts
        public async Task<List<Uri>> GetMyMetadata(Uri? relayUri = null, CancellationToken? token = null)
        {
            if (!TryGetNPub(out NPub? nPub) || nPub is null)
                return relayUri is null ? new(Relays.RunningRelaysUri) : new() { relayUri };
            return await GetPubkeyMetadata(nPub.Hex, relayUri, token);
        }
        public async Task<List<Uri>> GetPubkeyMetadata(string pubkeyHex, Uri? relayUri = null, CancellationToken? token = null)
        {
            if (relayUri is null)
                return await SendFilter(new NSRelayFilter(pubkeyHex, NKind.Metadata), token);

            if (!await SendFilter(relayUri, new NSRelayFilter(pubkeyHex, NKind.Metadata), token))
                return new() { relayUri };
            return new();
        }
        public async Task<List<Uri>> GetMyContacts(Uri? relayUri = null, CancellationToken? token = null)
        {
            if (!TryGetNPub(out NPub? nPub) || nPub is null)
                return relayUri is null ? new(Relays.RunningRelaysUri) : new() { relayUri };
            return await GetPubkeyContacts(nPub.Hex, relayUri, token);
        }
        public async Task<List<Uri>> GetPubkeyContacts(string pubkeyHex, Uri? relayUri = null, CancellationToken? token = null)
        {
            if (relayUri is null)
                return await SendFilter(new NSRelayFilter(pubkeyHex, NKind.Contacts), token);

            if (!await SendFilter(relayUri, new NSRelayFilter(pubkeyHex, NKind.Contacts), token))
                return new() { relayUri };
            return new();
        }
        #endregion


        #region Short Text Notes
        public async Task<List<Uri>> GetMyShortTextNotes(DateTime? startFrom = null, DateTime? endAt = null, Uri? relayUri = null, CancellationToken? token = null)
        {
            if (!TryGetNPub(out NPub? nPub) || nPub is null)
                return relayUri is null ? new(Relays.RunningRelaysUri) : new() { relayUri };
            return await GetContactShortTextNotes(nPub.Hex, startFrom, endAt, relayUri, token);
        }
        public async Task<List<Uri>> GetContactShortTextNotes(string pubkeyHex, DateTime? startFrom = null, DateTime? endAt = null, Uri? relayUri = null, CancellationToken? token = null)
        {
            return await GetContactsShortTextNotes(new List<string>() { pubkeyHex }, startFrom, endAt, relayUri, token);
        }
        public async Task<List<Uri>> GetContactsShortTextNotes(List<string> pubkeyHexes, DateTime? startFrom = null, DateTime? endAt = null, Uri? relayUri = null, CancellationToken? token = null)
        {
            if (relayUri is null)
                return await SendFilter(new NSRelayFilter(pubkeyHexes.ToArray(), NKind.ShortTextNote, startFrom, endAt), token);

            if (!await SendFilter(relayUri, new NSRelayFilter(pubkeyHexes.ToArray(), NKind.ShortTextNote, startFrom, endAt), token))
                return new() { relayUri };
            return new();
        }

        public async Task<List<Uri>> GetMyReservedNotes(DateTime? startFrom = null, DateTime? endAt = null, Uri? relayUri = null, CancellationToken? token = null)
        {
            if (!TryGetNPub(out NPub? nPub) || nPub is null)
                return relayUri is null ? new(Relays.RunningRelaysUri) : new() { relayUri };
            return await GetContactReservedNotes(nPub.Hex, startFrom, endAt, relayUri, token);
        }
        public async Task<List<Uri>> GetContactReservedNotes(string pubkeyHex, DateTime? startFrom = null, DateTime? endAt = null, Uri? relayUri = null, CancellationToken? token = null)
        {
            return await GetContactsReservedNotes(new List<string>() { pubkeyHex }, startFrom, endAt, relayUri, token);
        }
        public async Task<List<Uri>> GetContactsReservedNotes(List<string> pubkeyHexes, DateTime? startFrom = null, DateTime? endAt = null, Uri? relayUri = null, CancellationToken? token = null)
        {
            if (relayUri is null)
                return await SendFilter(new NSRelayFilter(pubkeyHexes.ToArray(), NKind.Reserved, startFrom, endAt), token);

            if (!await SendFilter(relayUri, new NSRelayFilter(pubkeyHexes.ToArray(), NKind.Reserved, startFrom, endAt), token))
                return new() { relayUri };
            return new();
        }
        #endregion


        #region Wallet Connect
        public async Task<bool> AskWalletConnectInfo(Uri relayUri, CancellationToken? token = null)
        {
            if (!await ConnectRelay(new(relayUri)))
                return false;
            return await SendFilter(relayUri, new NSRelayFilter(NKind.WalletInfo), token);
        }
        public async Task<bool> AskWalletResponse(Uri relayUri, CancellationToken? token = null)
        {
            if (!await ConnectRelay(new(relayUri)))
                return false;
            return await SendFilter(relayUri, new NSRelayFilter(NKind.WalletResponse), token);
        }
        #endregion


        public async Task<List<Uri>> GetEventById(string eventIdentifier, Uri? relayUri = null, CancellationToken? token = null)
        {
            if (relayUri is null)
                return await SendFilter(NSRelayFilter.FromEvent(eventIdentifier), token);

            if (!await SendFilter(relayUri, NSRelayFilter.FromEvent(eventIdentifier), token))
                return new() { relayUri };
            return new();
        }
        public async Task<List<Uri>> GetEventsReferredByThis(string eventIdentifier, Uri? relayUri = null, CancellationToken? token = null)
        {
            if (relayUri is null)
                return await SendFilter(NSRelayFilter.FromEventTag(eventIdentifier), token);

            if (!await SendFilter(relayUri, NSRelayFilter.FromEventTag(eventIdentifier), token))
                return new() { relayUri };
            return new();
        }


        #region Zaps
        /// <summary>
        /// Ask for a valid LN Invoice to the LN Service Provider.
        /// </summary>
        /// <param name="lnURLorADDRESS_Recipient">A valid LNURL or LNAddress</param>
        /// <param name="satoshisAmount">The amount expressed in Satoshi</param>
        /// <param name="recipientHexPubKey">The pubkey that will receive the zap</param>
        /// <param name="relayUrlForZapReceipt">A list of relays uri where you wish to receive the ZapReceipt event (kind 9735)</param>
        /// <param name="message">An optional string message to send along with the zap</param>
        /// <param name="eventId">An optional event Id to refer to</param>
        /// <param name="eventATag">An optional parametrized replaceable event coordinates</param>
        /// <param name="token">Optional cancellation token</param>
        /// <returns>A valid LNInvoice in case of success, or null in case of error</returns>
        public async Task<string?> GetLNInvoice(string lnURLorADDRESS_Recipient, decimal satoshisAmount, string recipientHexPubKey,
            List<string> relayUrlForZapReceipt, string? message = null, string? eventId = null, ATag? eventATag = null,
            CancellationToken? token = null)
        {
            try
            {
                if (!TryGetNSec(out NSec? nSec) || nSec is null)
                    return null;

                // Parse the LNURL or LNAddress to get the payment endpoint
                string? payEndpoint = NSUtilities.ParsePayEndpoitFromLNURLorADDRESS(lnURLorADDRESS_Recipient);
                if (string.IsNullOrEmpty(payEndpoint))
                    return null;
                // Ask the payment endpoint to give the ln service capabilities
                LNPayEndpointResponse? payEndpointResponse = await NSUtilities.FetchLNPayEndpoint(payEndpoint, token);
                if (payEndpointResponse is null)
                    return null;

                // If this ln service support nostr and has a valid pubkey
                if (!payEndpointResponse.AllowNostr || !payEndpointResponse.IsNostrPubKeyValid)
                    return null;

                // Creo un evento ZapRequest (kind 9734)
                string? lnurl = Bech32.Encode("lnurl", payEndpoint.UTF8AsByteArray());
                if (string.IsNullOrEmpty(lnurl))
                    return null;
                NEvent? zapRequest = NSEventMaker.ZapRequest(satoshisAmount, lnurl, recipientHexPubKey, relayUrlForZapReceipt, message, eventId, eventATag);
                if (zapRequest is null || !zapRequest.Sign(nSec))
                    return null;

                // Non lo pubblico sui relay, ma all'indirizzo contenuto nella proprietà "Callback" di payEndpointResponse
                string? ev = JsonConvert.SerializeObject(zapRequest, SerializerCustomSettings.Settings);
                if (string.IsNullOrEmpty(ev))
                    return null;

                LNZapRequestResponse? response = await NSUtilities.SendHttpZapRequest(payEndpointResponse.Callback, ev, satoshisAmount, lnurl, token);
                if (response is null)
                    return null;

                return response.Invoice;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 
        /// 
        /// Per poter richiedere un pagamento tramite Wallet Connect è prima necessario:
        ///     - ottenere l'uri e i parametri di Wallet Connect dal proprio provider lightning
        ///     - estrapolare le info dalla stringa
        ///     - richiedere gli eventi di tipo NKind.WalletInfo = 13194
        ///     - tramite l'evento OnWalletInfoReceived di questa classe si ricevono i metodo lightning possibili
        ///         con questa istanza di Wallet Connect
        ///     - E' NECESSARIO CHE CI SIA "pay_invoice" per proseguire
        /// 
        /// Chiamando questo metodo si deve passare l'uri di configurazione che è stato fornito dal wallet lightning.
        /// L'uri, che deve iniziare con "nostr+walletconnect:", viene letto e i parametri di connessione tramite Wallet
        /// Connect vengono letti.
        /// Viene quindi creato un evento NKind.WalletRequest (23194) il cui Content viene cifrato secondo il NIP-04 
        /// usando la proprietà "secret" ottenuta dall'uri indicato.
        /// L'evento viene inviato al relay indicato nell'uri indicato.
        /// 
        /// NOTA: L'url indicato viene letto e tenuto in memoria per tutta la durata di vita di questa istanza, per
        /// poter correttamente decryptare le risposte degli eventi NKind.WalletResponse = 23195
        /// </summary>
        /// <param name="walletConnectUri"></param>
        /// <param name="invoiceLN"></param>
        /// <returns></returns>
        public async Task<bool> SendWalletConnectPayRequest(string invoiceLN, CancellationToken? token = null)
        {
            try
            {
                if (!TryGetNSec(out NSec? nSec) || nSec is null
                    || WCParams is null || string.IsNullOrEmpty(WCParams.RelayUrl) || WCParams.WalletNSec is null)
                    return false;

                Uri wcRelayUri = new Uri(WCParams.RelayUrl);
                if (!await ConnectRelay(new(wcRelayUri)))
                    return false;

                NEvent? walletRequest = NSEventMaker.WalletRequestPayment(nSec, WCParams, invoiceLN);
                if (walletRequest is null || !walletRequest.Sign(nSec))
                    return false;

                if (!await SendEvent(wcRelayUri, walletRequest, token))
                    return false;

                return await AskWalletResponse(new(WCParams.RelayUrl));
            }
            catch
            {
                return false;
            }
        }
        #endregion


        #region Events
        private void Relay_OnInitialConnectionEstablished(Uri relayUri)
        {
            // I ask the relay for my contacts so i can set read/write permission on NSRelay instance
            //await GetMyContacts(relayUri);
            OnInitialConnectionEstablished?.Invoke(relayUri);
        }
        private void Relay_OnConnectionClosed(Uri relayUri, string reason)
        {
            OnConnectionClosed?.Invoke(relayUri, reason);
        }
        private void Relays_OnRelayMetadata(Uri relayUrl, RelayNIP11Metadata nip11Metadata)
        {
            OnRelayMetadata?.Invoke(relayUrl, nip11Metadata);
        }
        private void Relays_OnAuthResponse(Uri relayUri, NResponseAuth auth)
        {
            OnAuthResponse?.Invoke(relayUri, auth.ChallengeString);
        }
        private async void OnEventReceived(Uri relayUri, NResponseEvent ev)
        {
            if (ev.Event is null)
                return;

            OnEvent?.Invoke(relayUri, ev.Event);

            switch (ev.Event.Kind)
            {
                case NKind.Metadata:
                    UserMetadata? metadata = JsonConvert.DeserializeObject<UserMetadata>(ev.Event.Content ?? "") ?? null;
                    if (metadata is not null)
                        metadata.PubKey = ev.Event.PubKey ?? "";
                    OnMetadata?.Invoke(relayUri, metadata, ev.Event);
                    break;
                case NKind.ShortTextNote:
                    OnShortTextNote?.Invoke(relayUri, ev.Event);
                    break;
                case NKind.Reserved:
                    OnReserved?.Invoke(relayUri, ev.Event);
                    break;
                case NKind.Contacts:
                    List<RelayInfo> relaysInfo = JsonConvert.DeserializeObject<List<RelayInfo>>(ev.Event.Content ?? "", SerializerCustomSettings.Settings) ?? new();
                    // If this is my kind 3 event i want to set read/write permissions info on the corresponding relays
                    if (TryGetNPub(out NPub? npub) && npub is not null && ev.Event.PubKey == npub.Hex)
                        Relays.SetRelaysPermissions(relaysInfo);
                    OnContacts?.Invoke(relayUri, ev.Event, relaysInfo);
                    break;
                case NKind.EncryptedDm:
                    OnEncryptedDm?.Invoke(relayUri, ev.Event);
                    break;
                case NKind.Reaction:
                    OnReaction?.Invoke(relayUri, ev.Event);
                    break;
                case NKind.GenericRepost:
                    OnGenericRepost?.Invoke(relayUri, ev.Event);
                    break;
                case NKind.ZapReceipt:
                    OnZap?.Invoke(relayUri, ev.Event);
                    break;
                case NKind.LongFormContent:
                    OnLongFormContent?.Invoke(relayUri, ev.Event);
                    break;
                case NKind.WalletInfo:
                    if (!string.IsNullOrEmpty(ev.Event.Content))
                        OnWalletInfoReceived?.Invoke(relayUri, ev.Event.Content.Split(' '));
                    break;
                case NKind.WalletResponse:
                    if (WCParams is null || string.IsNullOrEmpty(WCParams.RelayUrl) || WCParams.WalletNSec is null)
                        break;
                    string decryptedContent = "";

                    if (!TryGetNSec(out NSec? nSec) || nSec is null)
                        break;
                    if (!TryGetNPub(out NPub? nPub) || nPub is null)
                        break;

                    if (_overrideDecryptionMethod is null)
                    {
                        try
                        {
                            decryptedContent = ev.Event.Decrypt(WCParams.WalletNSec) ?? "";
                        }
                        catch
                        {
                            try
                            {
                                decryptedContent = ev.Event.Decrypt(WCParams.WalletNSec, nPub) ?? "";
                            }
                            catch
                            {
                                try
                                {
                                    decryptedContent = ev.Event.Decrypt(NSec.FromBech32("nsec13w8vll9mwkcqlgw905cy6kue9n9a6dvp7u3ntqynjtxy3d53fpzswj0ps5")) ?? "";
                                }
                                catch
                                {
                                    //string a = "";
                                }
                            }
                        }

                    }
                    else
                        decryptedContent = await ev.Event.Decrypt(WCParams.WalletNSec, _overrideDecryptionMethod) ?? "";
                    WalletResponse? response = JsonConvert.DeserializeObject<WalletResponse>(decryptedContent, SerializerCustomSettings.Settings);
                    if (response is not null)
                        OnWalletResponseReceived?.Invoke(relayUri, response);
                    break;
            }
        }
        private void Relays_OnCount(Uri relayUri, NResponseCount count)
        {
            OnCount?.Invoke(relayUri, count.Result);
        }
        private void OnEose(Uri relayUri, NResponseEose eose)
        {
        }
        private void OnNotice(Uri relayUri, NResponseNotice notice)
        {
        }
        private void OnOk(Uri relayUri, NResponseOk ok)
        {
            if (ok.Accepted)
                OnEventApproved?.Invoke(relayUri, ok.EventId ?? "", ok.Message ?? "");
            else
                OnEventRefused?.Invoke(relayUri, ok.EventId ?? "", ok.Message ?? "");
        }
        private void OnUnknownMessage(Uri relayUri, NResponseUnknown unknown)
        {
        }
        private void OnRelayError(Uri relayUri, string error)
        {
            OnError?.Invoke(relayUri, error);
        }


        private void AttachEvents()
        {
            Relays.OnInitialConnectionEstablished += Relay_OnInitialConnectionEstablished;
            Relays.OnConnectionClosed += Relay_OnConnectionClosed;
            Relays.OnAuthResponse += Relays_OnAuthResponse;
            Relays.OnEvent += OnEventReceived;
            Relays.OnRelayMetadata += Relays_OnRelayMetadata;
            Relays.OnCount += Relays_OnCount;
            Relays.OnEose += OnEose;
            Relays.OnNotice += OnNotice;
            Relays.OnOk += OnOk;
            Relays.OnUnknownMessage += OnUnknownMessage;
            Relays.OnError += OnRelayError;
        }
        private void DetachEvents()
        {
            Relays.OnInitialConnectionEstablished -= Relay_OnInitialConnectionEstablished;
            Relays.OnConnectionClosed -= Relay_OnConnectionClosed;
            Relays.OnAuthResponse -= Relays_OnAuthResponse;
            Relays.OnEvent -= OnEventReceived;
            Relays.OnRelayMetadata -= Relays_OnRelayMetadata;
            Relays.OnCount -= Relays_OnCount;
            Relays.OnEose -= OnEose;
            Relays.OnNotice -= OnNotice;
            Relays.OnOk -= OnOk;
            Relays.OnUnknownMessage -= OnUnknownMessage;
            Relays.OnError -= OnRelayError;
        }
        #endregion


        public void Dispose()
        {
            DetachEvents();
            Relays?.Dispose();
            WCParams = null;
            if (UserRelaysConfig is not null)
                UserRelaysConfig.Clear();
            NPub = null;
            NSec = null;
        }
    }
}
