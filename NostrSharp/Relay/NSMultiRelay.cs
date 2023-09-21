using Newtonsoft.Json;
using NostrSharp.Relay.Models;
using NostrSharp.Relay.Models.Messagges;
using NostrSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NostrSharp.Relay
{
    public class NSMultiRelay : IDisposable
    {
        #region Events
        public delegate void EventHandler();
        public delegate void EventHandler<T1>(T1 p1);
        public delegate void EventHandler<T1, T2>(T1 p1, T2 p2);


        public event EventHandler<Uri> OnInitialConnectionEstablished;
        public event EventHandler<Uri> OnReconnectionAfterDisconnect;
        public event EventHandler<Uri> OnReconnectionAfterNoMessageReceived;
        public event EventHandler<Uri> OnReconnectionError;
        public event EventHandler<Uri> OnUserRequestedReconnection;
        public event EventHandler<Uri> OnServerRequestedReconnection;

        public event EventHandler<Uri, string> OnConnectionClosed;
        public event EventHandler<Uri> OnConnectionLost;
        public event EventHandler<Uri> OnDisconnectionAfterNoMessageReceived;
        public event EventHandler<Uri> OnConnectionError;
        public event EventHandler<Uri> OnUserRequestedDisconnection;
        public event EventHandler<Uri> OnServerRequestedDisconnection;

        public event EventHandler<Uri, RelayNIP11Metadata> OnRelayMetadata;
        public event EventHandler<Uri, NResponseAuth> OnAuthRequest;
        public event EventHandler<Uri, NResponseEvent> OnEvent;
        public event EventHandler<Uri, NResponseCount> OnCount;
        public event EventHandler<Uri, NResponseEose> OnEose;
        public event EventHandler<Uri, NResponseNotice> OnNotice;
        public event EventHandler<Uri, NResponseOk> OnOk;
        public event EventHandler<Uri, NResponseUnknown> OnUnknownMessage;
        public event EventHandler<Uri, string> OnError;
        #endregion


        private List<NSRelay> Relays { get; set; } = new List<NSRelay>();
        public List<Uri> RelaysUri => Relays is null ? new List<Uri>() : Relays.Select(x => x.Configurations.Uri).ToList();
        public List<Uri> RunningRelays => Relays is null ? new List<Uri>() : Relays.Where(x => x.IsRunning).Select(x => x.Configurations.Uri).ToList();


        private Dictionary<Uri, CancellationTokenSource> CancellationTokens { get; set; } = new();


        public NSMultiRelay() { }
        public NSMultiRelay(Uri relay) : this(new List<Uri>() { relay }) { }
        public NSMultiRelay(List<Uri> relaysUri) : this(relaysUri.Select(x => new NSRelayConfig(x, null, TimeSpan.FromSeconds(60), null)).ToList()) { }
        public NSMultiRelay(List<NSRelayConfig> relaysConfigs)
        {
            foreach (NSRelayConfig config in relaysConfigs)
                AddRelay(config);
        }


        public List<Uri> AddRelays(List<NSRelayConfig> relaysConfigs)
        {
            List<Uri> errorRelays = new();
            foreach (NSRelayConfig relayConfig in relaysConfigs)
                if (!AddRelay(relayConfig))
                    errorRelays.Add(relayConfig.Uri);
            return errorRelays;
        }
        public bool AddRelay(NSRelayConfig relayConfig)
        {
            try
            {
                CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
                NSRelay relay = new NSRelay(relayConfig, cancelTokenSource.Token);
                AttachEvents(relay);
                Relays.Add(relay);
                CancellationTokens.TryAdd(relayConfig.Uri, cancelTokenSource);
                return true;
            }
            catch { return false; }
        }


        /// <summary>
        /// Tenta di avviare tutti i Relay configurati.
        /// Restituisce una lista con i nomi dei Relay con cui non è stato possibile connettersi
        /// </summary>
        /// <returns></returns>
        public async Task<List<Uri>> RunAll()
        {
            List<Uri> nonRunningRelays = new();

            Parallel.ForEach(Relays, async (relay) =>
            {
                if (!await relay.TryRun())
                    nonRunningRelays.Add(relay.Configurations.Uri);
                else
                {
                    RelayNIP11Metadata? nip11RelayMetadata = await NSUtilities.GetNIP11RelayMetadata(relay.Configurations.Uri);
                    if (nip11RelayMetadata is not null)
                    {
                        relay.SetCapabilities(nip11RelayMetadata);
                        OnRelayMetadata?.Invoke(relay.Configurations.Uri, nip11RelayMetadata);
                    }
                }
            });
            return nonRunningRelays;
        }
        public async Task<bool> Run(Uri relayUri)
        {
            NSRelay? relay = GetRelayByName(relayUri);
            if (relay is null)
                return false;
            if (!await relay.TryRun())
                return false;
            RelayNIP11Metadata? nip11RelayMetadata = await NSUtilities.GetNIP11RelayMetadata(relayUri);
            if (nip11RelayMetadata is not null)
            {
                relay.SetCapabilities(nip11RelayMetadata);
                OnRelayMetadata?.Invoke(relay.Configurations.Uri, nip11RelayMetadata);
            }
            return true;
        }

        /// <summary>
        /// Tenta la disconnessione da tutti i Relay configurati.
        /// Restituisce una lista con i nomi dei Relay con cui non è stato possibile disconnettersi
        /// </summary>
        /// <returns></returns>
        public async Task<List<Uri>> StopAll()
        {
            List<Uri> nonStoppedRelays = new();
            foreach (NSRelay relay in Relays)
            {
                if (CancellationTokens.ContainsKey(relay.Configurations.Uri))
                    CancellationTokens[relay.Configurations.Uri].Cancel();
                if (!await relay.TryStop())
                    nonStoppedRelays.Add(relay.Configurations.Uri);
            }
            return nonStoppedRelays;
        }
        public async Task<bool> Stop(Uri relayUri)
        {
            NSRelay? relay = GetRelayByName(relayUri);
            if (relay is null)
                return false;
            if (CancellationTokens.ContainsKey(relayUri))
                CancellationTokens[relayUri].Cancel();
            bool result = await relay.TryStop();
            return result;
        }

        /// <summary>
        /// Tenta di riconnettersi con tutti i Relay configurati.
        /// Restituisce una lista con i nomi dei Relay con cui non è stato possibile riconnettersi
        /// </summary>
        /// <returns></returns>
        public async Task<List<Uri>> ReconnectAll()
        {
            List<Uri> nonReconnectedRelays = new();
            foreach (NSRelay relay in Relays)
                if (!await relay.TryReconnect())
                    nonReconnectedRelays.Add(relay.Configurations.Uri);
            return nonReconnectedRelays;
        }
        public async Task<bool> Reconnect(Uri relayUri)
        {
            NSRelay? relay = GetRelayByName(relayUri);
            if (relay is null)
                return false;
            return await relay.TryReconnect();
        }


        public async Task<bool> SendAuthentication(Uri relayUri, NRequestAuth request, CancellationToken? token = null)
        {
            return await Send(relayUri, JsonConvert.SerializeObject(request), token);
        }
        public async Task<List<Uri>> SendEvent(NRequestEvent request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request), token);
        }
        public async Task<List<Uri>> SendCountFilter(NRequestCount request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request), token);
        }
        public async Task<List<Uri>> SendFilter(NRequestReq request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request), token);
        }
        public async Task<List<Uri>> SendClose(NRequestClose request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request), token);
        }
        public async Task<bool> SendEvent(Uri relayUri, NRequestEvent request, CancellationToken? token = null)
        {
            return await Send(relayUri, JsonConvert.SerializeObject(request), token);
        }
        public async Task<bool> SendCountFilter(Uri relayUri, NRequestCount request, CancellationToken? token = null)
        {
            return await Send(relayUri, JsonConvert.SerializeObject(request), token);
        }
        public async Task<bool> SendFilter(Uri relayUri, NRequestReq request, CancellationToken? token = null)
        {
            return await Send(relayUri, JsonConvert.SerializeObject(request), token);
        }
        public async Task<bool> SendClose(Uri relayUri, NRequestClose request, CancellationToken? token = null)
        {
            return await Send(relayUri, JsonConvert.SerializeObject(request), token);
        }

        /// <summary>
        /// Invia una string a tutti i Relay connessi.
        /// Restituisce una lista con i nomi dei Relay a cui non è stato possibile inviare il messaggio
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<List<Uri>> Send(string msg, CancellationToken? token = null)
        {
            List<Uri> sendErrors = new();
            foreach (NSRelay relay in Relays)
                if (!await relay.Send(msg, token))
                    sendErrors.Add(relay.Configurations.Uri);
            return sendErrors;
        }
        /// <summary>
        /// Invia un byte[] a tutti i Relay connessi.
        /// Restituisce una lista con i nomi dei Relay a cui non è stato possibile inviare il messaggio
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<List<Uri>> Send(byte[] msg, CancellationToken? token = null)
        {
            List<Uri> sendErrors = new();
            foreach (NSRelay relay in Relays)
                if (!await relay.Send(msg, token))
                    sendErrors.Add(relay.Configurations.Uri);
            return sendErrors;
        }
        /// <summary>
        /// Invia un ArraySegment<byte> a tutti i Relay connessi.
        /// Restituisce una lista con i nomi dei Relay a cui non è stato possibile inviare il messaggio
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<List<Uri>> Send(ArraySegment<byte> msg, CancellationToken? token = null)
        {
            List<Uri> sendErrors = new();
            foreach (NSRelay relay in Relays)
                if (!await relay.Send(msg, token))
                    sendErrors.Add(relay.Configurations.Uri);
            return sendErrors;
        }
        /// <summary>
        /// Invia una string al relay indicato e restituisce un esito
        /// </summary>
        /// <param name="relayUri"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<bool> Send(Uri relayUri, string msg, CancellationToken? token = null)
        {
            NSRelay? relay = GetRelayByName(relayUri);
            if (relay is null)
                return false;
            return await relay.Send(msg, token);
        }
        /// <summary>
        /// Invia una string al relay indicato e restituisce un esito
        /// </summary>
        /// <param name="relayUri"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<bool> Send(Uri relayUri, byte[] msg, CancellationToken? token = null)
        {
            NSRelay? relay = GetRelayByName(relayUri);
            if (relay is null)
                return false;
            return await relay.Send(msg, token);
        }
        /// <summary>
        /// Invia una string al relay indicato e restituisce un esito
        /// </summary>
        /// <param name="relayUri"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<bool> Send(Uri relayUri, ArraySegment<byte> msg, CancellationToken? token = null)
        {
            NSRelay? relay = GetRelayByName(relayUri);
            if (relay is null)
                return false;
            return await relay.Send(msg, token);
        }


        #region Misc
        private NSRelay? GetRelayByName(Uri relayUri)
        {
            return Relays.FirstOrDefault(r => r.Name == relayUri.Host);
        }
        #endregion


        #region Events
        private void AttachEvents(NSRelay relay)
        {
            relay.OnInitialConnectionEstablished += Relay_OnInitialConnectionEstablished;
            relay.OnReconnectionAfterDisconnect += Relay_OnReconnectionAfterDisconnect;
            relay.OnReconnectionAfterNoMessageReceived += Relay_OnReconnectionAfterNoMessageReceived;
            relay.OnReconnectionError += Relay_OnReconnectionError;
            relay.OnUserRequestedReconnection += Relay_OnUserRequestedReconnection;
            relay.OnServerRequestedReconnection += Relay_OnServerRequestedReconnection;
            relay.OnConnectionClosed += Relay_OnConnectionClosed;
            relay.OnConnectionLost += Relay_OnConnectionLost;
            relay.OnDisconnectionAfterNoMessageReceived += Relay_OnDisconnectionAfterNoMessageReceived;
            relay.OnConnectionError += Relay_OnConnectionError;
            relay.OnUserRequestedDisconnection += Relay_OnUserRequestedDisconnection;
            relay.OnServerRequestedDisconnection += Relay_OnServerRequestedDisconnection;
            relay.OnAuthRequest += Relay_OnAuthRequest;
            relay.OnEvent += Relay_OnEvent;
            relay.OnCount += Relay_OnCount;
            relay.OnEose += Relay_OnEose;
            relay.OnNotice += Relay_OnNotice;
            relay.OnOk += Relay_OnOk;
            relay.OnUnknownMessage += Relay_OnUnknownMessage;
            relay.OnError += Relay_OnError;
        }
        private void DetachEvents(NSRelay relay)
        {
            relay.OnInitialConnectionEstablished -= Relay_OnInitialConnectionEstablished;
            relay.OnReconnectionAfterDisconnect -= Relay_OnReconnectionAfterDisconnect;
            relay.OnReconnectionAfterNoMessageReceived -= Relay_OnReconnectionAfterNoMessageReceived;
            relay.OnReconnectionError -= Relay_OnReconnectionError;
            relay.OnUserRequestedReconnection -= Relay_OnUserRequestedReconnection;
            relay.OnServerRequestedReconnection -= Relay_OnServerRequestedReconnection;
            relay.OnConnectionClosed -= Relay_OnConnectionClosed;
            relay.OnConnectionLost -= Relay_OnConnectionLost;
            relay.OnDisconnectionAfterNoMessageReceived -= Relay_OnDisconnectionAfterNoMessageReceived;
            relay.OnConnectionError -= Relay_OnConnectionError;
            relay.OnUserRequestedDisconnection -= Relay_OnUserRequestedDisconnection;
            relay.OnServerRequestedDisconnection -= Relay_OnServerRequestedDisconnection;
            relay.OnAuthRequest -= Relay_OnAuthRequest;
            relay.OnEvent -= Relay_OnEvent;
            relay.OnCount -= Relay_OnCount;
            relay.OnEose -= Relay_OnEose;
            relay.OnNotice -= Relay_OnNotice;
            relay.OnOk -= Relay_OnOk;
            relay.OnUnknownMessage -= Relay_OnUnknownMessage;
            relay.OnError -= Relay_OnError;
        }


        private void Relay_OnInitialConnectionEstablished(Uri relayUri)
        {
            OnInitialConnectionEstablished?.Invoke(relayUri);
        }
        private void Relay_OnReconnectionAfterDisconnect(Uri relayUri)
        {
            OnReconnectionAfterDisconnect?.Invoke(relayUri);
        }
        private void Relay_OnReconnectionAfterNoMessageReceived(Uri relayUri)
        {
            OnReconnectionAfterNoMessageReceived?.Invoke(relayUri);
        }
        private void Relay_OnReconnectionError(Uri relayUri)
        {
            OnReconnectionError?.Invoke(relayUri);
        }
        private void Relay_OnUserRequestedReconnection(Uri relayUri)
        {
            OnUserRequestedReconnection?.Invoke(relayUri);
        }
        private void Relay_OnServerRequestedReconnection(Uri relayUri)
        {
            OnServerRequestedReconnection?.Invoke(relayUri);
        }
        private void Relay_OnConnectionClosed(Uri relayUri, string reason)
        {
            OnConnectionClosed?.Invoke(relayUri, reason);
        }
        private void Relay_OnConnectionLost(Uri relayUri)
        {
            OnConnectionLost?.Invoke(relayUri);
        }
        private void Relay_OnDisconnectionAfterNoMessageReceived(Uri relayUri)
        {
            OnDisconnectionAfterNoMessageReceived?.Invoke(relayUri);
        }
        private void Relay_OnConnectionError(Uri relayUri)
        {
            OnConnectionError?.Invoke(relayUri);
        }
        private void Relay_OnUserRequestedDisconnection(Uri relayUri)
        {
            OnUserRequestedDisconnection?.Invoke(relayUri);
        }
        private void Relay_OnServerRequestedDisconnection(Uri relayUri)
        {
            OnServerRequestedDisconnection?.Invoke(relayUri);
        }


        private void Relay_OnAuthRequest(Uri relayUri, NResponseAuth response)
        {
            OnAuthRequest?.Invoke(relayUri, response);
        }
        private void Relay_OnEvent(Uri relayUri, NResponseEvent response)
        {
            OnEvent?.Invoke(relayUri, response);
        }
        private void Relay_OnCount(Uri relayUri, NResponseCount response)
        {
            OnCount?.Invoke(relayUri, response);
        }
        private void Relay_OnEose(Uri relayUri, NResponseEose response)
        {
            OnEose?.Invoke(relayUri, response);
        }
        private void Relay_OnNotice(Uri relayUri, NResponseNotice response)
        {
            OnNotice?.Invoke(relayUri, response);
        }
        private void Relay_OnOk(Uri relayUri, NResponseOk response)
        {
            OnOk?.Invoke(relayUri, response);
        }
        private void Relay_OnUnknownMessage(Uri relayUri, NResponseUnknown response)
        {
            OnUnknownMessage?.Invoke(relayUri, response);
        }
        private void Relay_OnError(Uri relayUri, string error)
        {
            OnError?.Invoke(relayUri, error);
        }
        #endregion


        public void Dispose()
        {
            foreach (NSRelay relay in Relays)
            {
                DetachEvents(relay);
                CancellationTokens[relay.Configurations.Uri].Cancel();
                relay.Dispose();
            }
            CancellationTokens.Clear();
            Relays.Clear();
        }
    }
}
