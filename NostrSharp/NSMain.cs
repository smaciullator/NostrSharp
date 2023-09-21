using Newtonsoft.Json;
using NostrSharp.Cryptography;
using NostrSharp.Extensions;
using NostrSharp.Keys;
using NostrSharp.Models.LN;
using NostrSharp.Nostr;
using NostrSharp.Nostr.Enums;
using NostrSharp.Nostr.Models;
using NostrSharp.Nostr.Models.Tags;
using NostrSharp.Relay;
using NostrSharp.Relay.Models;
using NostrSharp.Relay.Models.Messagges;
using NostrSharp.Settings;
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

        public event EventHandler<Uri, RelayNIP11Metadata> OnRelayMetadata;
        public event EventHandler<Uri, string?> OnAuthRequest;
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
        public Dictionary<Uri, bool> RelaysStatus { get; private set; } = new();
        public Dictionary<Uri, Guid> RelaysSubscriptionId { get; private set; } = new();


        private List<RelayInfo> UserRelaysInfo { get; set; } = new();
        private WalletConnect? WCParams { get; set; } = default;
        private static SemaphoreSlim _semaphore { get; set; } = new SemaphoreSlim(1);


        public NSMain() { }


        /// <summary>
        /// Vale sia per le NPub che per le NSec, in entrambi i formati Hex o Bech32
        /// </summary>
        /// <param name="key"></param>
        public void Init(string key)
        {
            try { Init(NPub.FromBech32(key)); }
            catch
            {
                try { Init(NPub.FromHex(key)); }
                catch
                {
                    try { Init(NSec.FromBech32(key)); }
                    catch
                    {
                        try { Init(NSec.FromHex(key)); }
                        catch { }
                    }
                }
            }
        }
        public void Init(string? npub, string? nsec)
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

            Init(NPub, NSec);
        }
        public void Init(NPub? nPub)
        {
            NPub = nPub;
            NSec = null;
        }
        public void Init(NSec? nSec)
        {
            NPub = null;
            NSec = nSec;
        }
        public void Init(NPub? nPub, NSec? nSec)
        {
            NPub = nPub;
            NSec = nSec;
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


        public async Task<List<Uri>?> ConnectRelays(List<NSRelayConfig> relays)
        {
            relays.RemoveAll(x => Relays.RunningRelays.Any(y => y == x.Uri));
            if (relays.Count == 0)
                return null;
            Relays = new NSMultiRelay(relays);

            RelaysStatus.Clear();
            foreach (NSRelayConfig cfg in relays)
                RelaysStatus.TryAdd(cfg.Uri, false);

            AttachEvents();
            return await Relays.RunAll();
        }
        public async Task<List<Uri>> DisconnectRelays()
        {
            List<Uri> errors = new();
            foreach (Uri relayUri in Relays.RunningRelays)
                if (!await DisconnectRelay(relayUri))
                    errors.Add(relayUri);
            return errors;
        }
        public async Task<bool> DisconnectRelay(Uri relayUri)
        {
            if (!IsRelayRunning(relayUri))
                return false;
            if (RelaysSubscriptionId.ContainsKey(relayUri))
                await Relays.SendClose(relayUri, new NRequestClose(RelaysSubscriptionId[relayUri].ToString()));
            return await Relays.Stop(relayUri);
        }
        public bool AddRelay(Uri relayUri)
        {
            return Relays.AddRelay(new NSRelayConfig(relayUri));
        }
        public bool IsRelayConnected(Uri relayUri)
        {
            if (RelaysStatus.ContainsKey(relayUri))
                return RelaysStatus[relayUri];
            return false;
        }
        public bool IsRelayRunning(Uri relayUri)
        {
            if (Relays.RunningRelays.Contains(relayUri))
                return RelaysStatus[relayUri];
            return false;
        }


        public RelayPermissions? GetRelayPermissions(Uri relayUri)
        {
            RelayInfo? permissions = UserRelaysInfo.FirstOrDefault(x => x.RelayUri == relayUri.ToString());
            if (permissions is null)
                return null;
            return permissions.RelayPermissions;
        }


        public async Task<bool> SendAuthentication(Uri relayUri, string challengeString, CancellationToken? token = null)
        {
            if (!TryGetNSec(out NSec? nSec) || nSec is null)
                return false;

            NEvent authEvent = NSEventMaker.Authentication(relayUri, challengeString);
            if (!IsRelayRunning(relayUri) || !authEvent.Sign(nSec))
                return false;
            return await Relays.SendAuthentication(relayUri, new NRequestAuth(authEvent), token);
        }
        public async Task<List<Uri>> SendFilter(NSRelayFilter filters, CancellationToken? token = null)
        {
            List<Uri> errors = new();
            foreach (Uri relayUri in Relays.RunningRelays)
                if (!await SendFilter(relayUri, filters, token))
                    errors.Add(relayUri);
            return errors;
        }
        public async Task<bool> SendFilter(Uri relayUri, NSRelayFilter filters, CancellationToken? token = null)
        {
            if (!IsRelayRunning(relayUri))
                return false;
            try
            {
                _semaphore.Wait();
                if (!RelaysSubscriptionId.ContainsKey(relayUri))
                    RelaysSubscriptionId.TryAdd(relayUri, Guid.NewGuid());
                string subId = RelaysSubscriptionId[relayUri].ToString();
                _semaphore.Release();
                return await Relays.SendFilter(relayUri, new NRequestReq(subId, filters), token);
            }
            catch (Exception ex)
            {
                _semaphore.Release();
                return false;
            }
        }
        public async Task<List<Uri>> SendCount(NSRelayFilter filters, CancellationToken? token = null)
        {
            List<Uri> errors = new();
            foreach (Uri relayUri in Relays.RunningRelays)
                if (!await SendCount(relayUri, filters, token))
                    errors.Add(relayUri);
            return errors;
        }
        public async Task<bool> SendCount(Uri relayUri, NSRelayFilter filters, CancellationToken? token = null)
        {
            if (!IsRelayRunning(relayUri))
                return false;
            try
            {
                _semaphore.Wait();
                if (!RelaysSubscriptionId.ContainsKey(relayUri))
                    RelaysSubscriptionId.TryAdd(relayUri, Guid.NewGuid());
                string subId = RelaysSubscriptionId[relayUri].ToString();
                _semaphore.Release();
                return await Relays.SendCountFilter(relayUri, new NRequestCount(subId, filters), token);
            }
            catch (Exception ex)
            {
                _semaphore.Release();
                return false;
            }
        }
        public async Task<List<Uri>> SendEvent(NEvent ev, CancellationToken? token = null)
        {
            List<Uri> errors = new();
            foreach (Uri relayUri in Relays.RunningRelays)
                if (!await SendEvent(relayUri, ev, token))
                    errors.Add(relayUri);
            return errors;
        }
        public async Task<bool> SendEvent(Uri relayUri, NEvent ev, CancellationToken? token = null)
        {
            if (!TryGetNSec(out NSec? nSec) || nSec is null)
                return false;

            RelayPermissions? permissions = GetRelayPermissions(relayUri);
            if (!IsRelayRunning(relayUri) || !ev.Sign(nSec) || (permissions is not null && !permissions.Write))
                return false;
            return await Relays.SendEvent(relayUri, new NRequestEvent(ev), token);
        }


        #region Metadata + Contacts
        public async Task<List<Uri>> GetMyMetadata(Uri? relayUri = null, CancellationToken? token = null)
        {
            if (!TryGetNPub(out NPub? nPub) || nPub is null)
                return relayUri is null ? new(Relays.RunningRelays) : new() { relayUri };
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
                return relayUri is null ? new(Relays.RunningRelays) : new() { relayUri };
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
                return relayUri is null ? new(Relays.RunningRelays) : new() { relayUri };
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
                return relayUri is null ? new(Relays.RunningRelays) : new() { relayUri };
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
        /// Restituisce l'invoice da pagare in caso di successo, o null in caso di errore
        /// </summary>
        /// <param name="lnURLorADDRESS"></param>
        /// <param name="satoshisAmount"></param>
        /// <param name="recipientHexPubKey"></param>
        /// <param name="relayUrlForZapReceipt"></param>
        /// <param name="message"></param>
        /// <param name="eventId"></param>
        /// <param name="eventATag"></param>
        /// <returns></returns>
        public async Task<string?> SendZapRequest(string lnURLorADDRESS, decimal satoshisAmount, string recipientHexPubKey, List<string> relayUrlForZapReceipt,
            string? message, string? eventId, ATag? eventATag, CancellationToken? token = null)
        {
            try
            {
                if (!TryGetNSec(out NSec? nSec) || nSec is null)
                    return null;

                string? payEndpoint = NSUtilities.ParsePayEndpoitFromLNURLorADDRESS(lnURLorADDRESS);
                if (string.IsNullOrEmpty(payEndpoint))
                    return null;
                LNPayEndpointResponse? payEndpointResponse = await NSUtilities.FetchLNPayEndpoint(payEndpoint);
                if (payEndpointResponse is null)
                    return null;

                if (!payEndpointResponse.AllowNostr || string.IsNullOrEmpty(payEndpointResponse.NostrPubKey))
                    return null;

                // La pubkey del nodo lightning deve essere un hex valido
                try { NPub.FromHex(payEndpointResponse.NostrPubKey); }
                catch { return null; }

                byte[] encodedBody = payEndpoint.UTF8AsByteArray();
                string? lnurl = Bech32.Encode("lnurl", encodedBody);
                if (string.IsNullOrEmpty(lnurl))
                    return null;

                NEvent? zapRequest = NSEventMaker.ZapRequest(satoshisAmount, lnurl, recipientHexPubKey, relayUrlForZapReceipt, message, eventId, eventATag);
                if (zapRequest is null || !zapRequest.Sign(nSec))
                    return null;
                string? ev = JsonConvert.SerializeObject(zapRequest, SerializerCustomSettings.Settings);
                if (string.IsNullOrEmpty(ev))
                    return null;

                LNZapRequestResponse? zapResponse = await NSUtilities.FetchLNZapResponse(payEndpointResponse.Callback, ev, satoshisAmount, lnurl);
                if (!string.IsNullOrEmpty(zapResponse.Status) || string.IsNullOrEmpty(zapResponse.Invoice))
                    return null;

                return zapResponse.Invoice;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        /// <summary>
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
        /// <param name="invoicLN"></param>
        /// <returns></returns>
        public async Task<bool> SendWalletConnectPay(Uri walletConnectUri, string invoicLN, CancellationToken? token = null)
        {
            try
            {
                if (!TryGetNSec(out NSec? nSec) || nSec is null)
                    return false;

                WCParams = NSUtilities.ReadNIP47WalletConnectUri(walletConnectUri);
                if (WCParams is null || string.IsNullOrEmpty(WCParams.RelayUrl) || WCParams.WalletNSec is null)
                    return false;

                NEvent? walletRequest = NSEventMaker.WalletRequestPayment(WCParams, invoicLN);
                if (walletRequest is null)
                    return false;

                Uri wcRelayUri = new Uri(WCParams.RelayUrl);
                if (!AddRelay(wcRelayUri))
                    return false;

                return await SendEvent(wcRelayUri, walletRequest, token);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion


        #region Events
        private void Relay_OnInitialConnectionEstablished(Uri relayUri)
        {
            RelaysStatus[relayUri] = true;
            OnInitialConnectionEstablished?.Invoke(relayUri);
        }
        private void OnConnectionClosed(Uri relayUri, string reason)
        {
            RelaysStatus[relayUri] = false;
            try
            {
                _semaphore.Wait();
                if (RelaysSubscriptionId.ContainsKey(relayUri))
                    RelaysSubscriptionId.Remove(relayUri);
                _semaphore.Release();
            }
            catch (Exception ex)
            {
                _semaphore.Release();
            }
        }
        private void Relays_OnRelayMetadata(Uri relayUrl, RelayNIP11Metadata nip11Metadata)
        {
            OnRelayMetadata?.Invoke(relayUrl, nip11Metadata);
        }
        private void Relays_OnAuthRequest(Uri relayUri, NResponseAuth auth)
        {
            OnAuthRequest?.Invoke(relayUri, auth.ChallengeString);
        }
        private void OnEventReceived(Uri relayUri, NResponseEvent ev)
        {
            if (ev.Event is null)
                return;
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
                    UserRelaysInfo = new(relaysInfo);
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
                case NKind.Zap:
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
                    WalletResponse? response = JsonConvert.DeserializeObject<WalletResponse>(ev.Event.Decrypt(WCParams.WalletNSec) ?? "", SerializerCustomSettings.Settings);
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
            Relays.OnConnectionClosed += OnConnectionClosed;
            Relays.OnAuthRequest += Relays_OnAuthRequest;
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
            Relays.OnConnectionClosed -= OnConnectionClosed;
            Relays.OnAuthRequest -= Relays_OnAuthRequest;
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
            _semaphore.Wait();
            DetachEvents();
            RelaysStatus?.Clear();
            RelaysSubscriptionId?.Clear();
            Relays?.Dispose();
            WCParams = null;
            if (UserRelaysInfo is not null)
                UserRelaysInfo.Clear();
            UserRelaysInfo = null;
            NPub = null;
            NSec = null;
            _semaphore.Release();
            _semaphore.Dispose();
        }
    }
}
