using Newtonsoft.Json;
using NostrSharp.Json;
using NostrSharp.Relay.Models;
using NostrSharp.Relay.Models.Messagges;
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
        public event EventHandler<Uri, NResponseAuth> OnAuthResponse;
        public event EventHandler<Uri, NResponseEvent> OnEvent;
        public event EventHandler<Uri, NResponseCount> OnCount;
        public event EventHandler<Uri, NResponseEose> OnEose;
        public event EventHandler<Uri, NResponseNotice> OnNotice;
        public event EventHandler<Uri, NResponseOk> OnOk;
        public event EventHandler<Uri, NResponseUnknown> OnUnknownMessage;
        public event EventHandler<Uri, string> OnError;
        #endregion


        public List<NSRelay> Relays { get; private set; } = new List<NSRelay>();
        public List<Uri> RelaysUri => Relays is null ? new List<Uri>() : Relays.Select(x => x.Configurations.Uri).ToList();
        public List<NSRelay> RunningRelays => Relays is null ? new List<NSRelay>() : Relays.Where(x => x.IsRunning).ToList();
        public List<Uri> RunningRelaysUri => Relays is null ? new List<Uri>() : Relays.Where(x => x.IsRunning).Select(x => x.Configurations.Uri).ToList();
        public List<NSRelay> NonRunningRelays => Relays is null ? new List<NSRelay>() : Relays.Where(x => !x.IsRunning).ToList();
        public List<Uri> NonRunningRelaysUri => Relays is null ? new List<Uri>() : Relays.Where(x => !x.IsRunning).Select(x => x.Configurations.Uri).ToList();


        public NSMultiRelay() { }
        public NSMultiRelay(Uri relay) : this(new List<Uri>() { relay }) { }
        public NSMultiRelay(List<Uri> relaysUri) : this(relaysUri.Select(x => new NSRelayConfig(x, null, TimeSpan.FromSeconds(60), null)).ToList()) { }
        public NSMultiRelay(List<NSRelayConfig> relaysConfigs)
        {
            foreach (NSRelayConfig config in relaysConfigs)
                AddRelay(config);
        }


        #region Connection
        /// <summary>
        /// Add a new list of relays instance to the internal list of relays.
        /// NOTE: this method will NOT start the connections with the relays.
        /// If you want to start the connections you have to call "ConnectAll()" method
        /// </summary>
        /// <param name="relaysConfigs"></param>
        /// <returns></returns>
        public List<Uri> AddRelays(List<NSRelayConfig> relaysConfigs)
        {
            List<Uri> errorRelays = new();
            foreach (NSRelayConfig relayConfig in relaysConfigs)
                if (!AddRelay(relayConfig))
                    errorRelays.Add(relayConfig.Uri);
            return errorRelays;
        }
        /// <summary>
        /// Add a new relay instance to the internal list of relays.
        /// NOTE: this method will NOT start the connection with the relay.
        /// If you want to start the connection you have to call "ConnectAll()" or "Connect()" methods
        /// </summary>
        /// <param name="relayConfig"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns></returns>
        public bool AddRelay(NSRelayConfig relayConfig, CancellationTokenSource? cancellationTokenSource = null)
        {
            try
            {
                if (Relays.Any(x => x.Uri == relayConfig.Uri))
                    return true;

                NSRelay relay = new NSRelay(relayConfig, cancellationTokenSource);
                AttachEvents(relay);
                Relays.Add(relay);
                return true;
            }
            catch { return false; }
        }


        /// <summary>
        /// Try to run a connection with all the already added relays
        /// </summary>
        /// <returns>Return a list with the uris that has given any error during the connection</returns>
        public async Task<List<Uri>> ConnectAll()
        {
            List<Uri> nonRunningRelays = new();
            Parallel.ForEach(Relays, async (relay) =>
            {
                if (!await relay.TryConnect())
                    nonRunningRelays.Add(relay.Configurations.Uri);
            });
            return nonRunningRelays;
        }
        /// <summary>
        /// Try to run a relay connection given it's uri.
        /// NOTE: the relay should be added using "AddRelay" before calling this method, otherwise this method return false;
        /// </summary>
        /// <param name="relayUri"></param>
        /// <returns>True if everything go smooth or if the connection was already running, false otherwise</returns>
        public async Task<bool> Connect(Uri relayUri)
        {
            NSRelay? relay = GetRelayByUri(relayUri);
            if (relay is null)
                return false;
            return await relay.TryConnect();
        }
        /// <summary>
        /// Try to stop all the relays connections
        /// </summary>
        /// <returns>Return a list with the uris that has given any error during the disconnection</returns>
        public async Task<List<Uri>> DisconnectAll()
        {
            List<Uri> nonStoppedRelays = new();
            foreach (NSRelay relay in Relays)
            {
                if (!await relay.TryDisconnect())
                    nonStoppedRelays.Add(relay.Configurations.Uri);
                else
                    DetachEvents(relay);
            }
            return nonStoppedRelays;
        }
        /// <summary>
        /// Try to stop a relay connection given it's uri.
        /// NOTE: the relay should be added using "AddRelay" before calling this method, otherwise this method return false;
        /// </summary>
        /// <param name="relayUri"></param>
        /// <returns>True if everything go smooth or if the connection was already stopped, false otherwise</returns>
        public async Task<bool> Disconnect(Uri relayUri)
        {
            NSRelay? relay = GetRelayByUri(relayUri);
            if (relay is null)
                return false;
            DetachEvents(relay);
            return await relay.TryDisconnect();
        }
        /// <summary>
        /// Try to stop and start the connection with all the configured relays.
        /// </summary>
        /// <returns>Return a list with the uris that has given any error during the process</returns>
        public async Task<List<Uri>> ReconnectAll()
        {
            List<Uri> nonReconnectedRelays = new();
            foreach (NSRelay relay in Relays)
            {
                if (!await relay.TryReconnect())
                    nonReconnectedRelays.Add(relay.Configurations.Uri);
                else
                    AttachEvents(relay);
            }
            return nonReconnectedRelays;
        }
        /// <summary>
        /// Try to stop and start the connection with the specified relay
        /// </summary>
        /// <param name="relayUri"></param>
        /// <returns>True if everything go smooth, false otherwise</returns>
        public async Task<bool> Reconnect(Uri relayUri)
        {
            NSRelay? relay = GetRelayByUri(relayUri);
            if (relay is null)
                return false;
            bool reconnected = await relay.TryReconnect();
            if (reconnected)
                AttachEvents(relay);
            return reconnected;
        }
        #endregion


        #region Send
        /// <summary>
        /// Try to send an Authentication request to a specific relay by it's uri.
        /// </summary>
        /// <param name="relayUri"></param>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendAuthentication(Uri relayUri, NRequestAuth request, CancellationToken? token = null)
        {
            return await Send(relayUri, JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }

        /// <summary>
        /// Try to send an event to all the connected relays.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>Return a list with the uris that has given some error during the send</returns>
        public async Task<List<Uri>> SendEvent(NRequestEvent request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }
        /// <summary>
        /// Try to send an event to a specific relay by it's uri.
        /// </summary>
        /// <param name="relayUri"></param>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendEvent(Uri relayUri, NRequestEvent request, CancellationToken? token = null)
        {
            return await Send(relayUri, JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }

        /// <summary>
        /// Try to send a filter request to all the connected relays.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>Return a list with the uris that has given some error during the send</returns>
        public async Task<List<Uri>> SendFilter(NRequestReq request, CancellationToken? token = null)
        {
            List<Uri> sendErrors = new();
            foreach (NSRelay relay in RunningRelays)
                if (!await SendFilter(relay.Configurations.Uri, request, token))
                    sendErrors.Add(relay.Configurations.Uri);
            return sendErrors;
        }
        /// <summary>
        /// Try to send a filter request to a specific relay by it's uri.
        /// </summary>
        /// <param name="relayUri"></param>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendFilter(Uri relayUri, NRequestReq request, CancellationToken? token = null)
        {
            if (!CheckSubscriptionId(relayUri, request))
                return false;
            return await Send(relayUri, JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }

        /// <summary>
        /// Try to send a count filter request to all the connected relays.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>Return a list with the uris that has given some error during the send</returns>
        public async Task<List<Uri>> SendCount(NRequestCount request, CancellationToken? token = null)
        {
            List<Uri> sendErrors = new();
            foreach (NSRelay relay in RunningRelays)
                if (!await SendCount(relay.Configurations.Uri, request, token))
                    sendErrors.Add(relay.Configurations.Uri);
            return sendErrors;
        }
        /// <summary>
        /// Try to send a count filter request to a specific relay by it's uri.
        /// </summary>
        /// <param name="relayUri"></param>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendCount(Uri relayUri, NRequestCount request, CancellationToken? token = null)
        {
            if (!CheckSubscriptionId(relayUri, request))
                return false;
            return await Send(relayUri, JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }

        /// <summary>
        /// Try to send a close request to all the connected relays.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>Return a list with the uris that has given some error during the send</returns>
        public async Task<List<Uri>> SendClose(NRequestClose request, CancellationToken? token = null)
        {
            List<Uri> sendErrors = new();
            foreach (NSRelay relay in RunningRelays)
                if (!await SendClose(relay.Configurations.Uri, request, token))
                    sendErrors.Add(relay.Configurations.Uri);
            return sendErrors;
        }
        /// <summary>
        /// Try to send a close request to a specific relay by it's uri.
        /// </summary>
        /// <param name="relayUri"></param>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendClose(Uri relayUri, NRequestClose request, CancellationToken? token = null)
        {
            if (!CheckSubscriptionId(relayUri, request))
                return false;
            return await Send(relayUri, JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }


        public async Task<List<Uri>> Send(string msg, CancellationToken? token = null)
        {
            List<Uri> sendErrors = new();
            foreach (NSRelay relay in RunningRelays)
                if (!await relay.Send(msg, token))
                    sendErrors.Add(relay.Configurations.Uri);
            return sendErrors;
        }
        public async Task<bool> Send(Uri relayUri, string msg, CancellationToken? token = null)
        {
            NSRelay? relay = GetRelayByUri(relayUri);
            if (relay is null)
                return false;
            return await relay.Send(msg, token);
        }

        public async Task<List<Uri>> Send(byte[] msg, CancellationToken? token = null)
        {
            List<Uri> sendErrors = new();
            foreach (NSRelay relay in RunningRelays)
                if (!await relay.Send(msg, token))
                    sendErrors.Add(relay.Configurations.Uri);
            return sendErrors;
        }
        public async Task<bool> Send(Uri relayUri, byte[] msg, CancellationToken? token = null)
        {
            NSRelay? relay = GetRelayByUri(relayUri);
            if (relay is null)
                return false;
            return await relay.Send(msg, token);
        }

        public async Task<List<Uri>> Send(ArraySegment<byte> msg, CancellationToken? token = null)
        {
            List<Uri> sendErrors = new();
            foreach (NSRelay relay in RunningRelays)
                if (!await relay.Send(msg, token))
                    sendErrors.Add(relay.Configurations.Uri);
            return sendErrors;
        }
        public async Task<bool> Send(Uri relayUri, ArraySegment<byte> msg, CancellationToken? token = null)
        {
            NSRelay? relay = GetRelayByUri(relayUri);
            if (relay is null)
                return false;
            return await relay.Send(msg, token);
        }
        #endregion


        #region Misc
        /// <summary>
        /// Set the read/write permissions for the relays list specified
        /// </summary>
        /// <param name="permissions"></param>
        public void SetRelaysPermissions(List<RelayInfo> relayInfos)
        {
            foreach (RelayInfo info in relayInfos)
            {
                NSRelay? relay = GetRelayByUri(new(info.RelayUri));
                if (relay is not null)
                    relay.SetRelayPermissions(info.RelayPermissions);
            }
        }


        private NSRelay? GetRelayByUri(Uri relayUri)
        {
            return Relays.FirstOrDefault(r => r.Uri == relayUri);
        }
        private Guid? GetRelaySubscriptionIdByUri(Uri relayUri)
        {
            NSRelay? relay = GetRelayByUri(relayUri);
            if (relay is null)
                return null;
            return relay.SubscriptionId;
        }


        private bool CheckSubscriptionId(Uri relayUri, NRequestReq request)
        {
            if (string.IsNullOrEmpty(request.SubscriptionId) || !Guid.TryParse(request.SubscriptionId, out _))
            {
                Guid? subscriptionId = GetRelaySubscriptionIdByUri(relayUri);
                if (subscriptionId is null)
                    return false;
                request.SubscriptionId = subscriptionId.ToString() ?? "";
                if (string.IsNullOrEmpty(request.SubscriptionId) || !Guid.TryParse(request.SubscriptionId, out _))
                    return false;
            }
            return true;
        }
        private bool CheckSubscriptionId(Uri relayUri, NRequestCount request)
        {
            if (string.IsNullOrEmpty(request.SubscriptionId) || !Guid.TryParse(request.SubscriptionId, out _))
            {
                Guid? subscriptionId = GetRelaySubscriptionIdByUri(relayUri);
                if (subscriptionId is null)
                    return false;
                request.SubscriptionId = subscriptionId.ToString() ?? "";
                if (string.IsNullOrEmpty(request.SubscriptionId) || !Guid.TryParse(request.SubscriptionId, out _))
                    return false;
            }
            return true;
        }
        private bool CheckSubscriptionId(Uri relayUri, NRequestClose request)
        {
            if (string.IsNullOrEmpty(request.SubscriptionId) || !Guid.TryParse(request.SubscriptionId, out _))
            {
                Guid? subscriptionId = GetRelaySubscriptionIdByUri(relayUri);
                if (subscriptionId is null)
                    return false;
                request.SubscriptionId = subscriptionId.ToString() ?? "";
                if (string.IsNullOrEmpty(request.SubscriptionId) || !Guid.TryParse(request.SubscriptionId, out _))
                    return false;
            }
            return true;
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
            relay.OnRelayMetadata += Relay_OnRelayMetadata;
            relay.OnAuthResponse += Relay_OnAuthResponse;
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
            relay.OnRelayMetadata -= Relay_OnRelayMetadata;
            relay.OnAuthResponse -= Relay_OnAuthResponse;
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
        private void Relay_OnRelayMetadata(Uri relayUri, RelayNIP11Metadata relayCapabilities)
        {
            OnRelayMetadata?.Invoke(relayUri, relayCapabilities);
        }


        private void Relay_OnAuthResponse(Uri relayUri, NResponseAuth response)
        {
            OnAuthResponse?.Invoke(relayUri, response);
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
                DetachEvents(relay);
            Relays.Clear();
        }
    }
}
