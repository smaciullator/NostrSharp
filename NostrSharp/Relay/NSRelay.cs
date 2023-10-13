using Newtonsoft.Json;
using NostrSharp.Json;
using NostrSharp.Relay.Enums;
using NostrSharp.Relay.Models;
using NostrSharp.Relay.Models.Messagges;
using NostrSharp.Tools;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NostrSharp.Relay
{
    public class NSRelay : IDisposable
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


        public NSRelayConfig Configurations { get; private set; }
        private ClientWebSocket WSC { get; set; } = new ClientWebSocket();
        public CancellationTokenSource CancelToken { get; private set; }
        public Guid SubscriptionId { get; set; } = Guid.NewGuid();


        public RelayNIP11Metadata Capabilities { get; private set; } = new();
        public Uri Uri { get; init; }
        public string HostName { get; init; }
        /// <summary>
        /// True if the Web Socket is connected
        /// </summary>
        public bool IsRunning => WSC is not null ? WSC.State == WebSocketState.Open : false;


        private ConcurrentQueue<byte[]> _receivedData = new();


        /// <summary>
        /// Initialize this relay connection instance with an optional CancellationToken
        /// </summary>
        /// <param name="config">Must contain a valid uri for the relay</param>
        /// <param name="cancellationTokenSource"></param>
        public NSRelay(NSRelayConfig config, CancellationTokenSource? cancellationTokenSource = null)
        {
            if (config.Uri.ToString().EndsWith("/"))
                config.Uri = new Uri(config.Uri.ToString().Substring(0, config.Uri.ToString().Length - 1));

            if (cancellationTokenSource is null)
                cancellationTokenSource = new CancellationTokenSource();
            Configurations = config;
            Uri = config.Uri;
            HostName = config.Uri.Host;
            this.CancelToken = cancellationTokenSource;
        }


        /// <summary>
        /// Try to start a Web Socket connection with the configured relay.
        /// </summary>
        /// <returns>True if everything go smooth or if the connection was already running, false otherwise</returns>
        public async Task<bool> TryConnect()
        {
            if (Configurations is null)
                return false;
            if (IsRunning)
                return true;

            try
            {
                SubscriptionId = Guid.NewGuid();

                CancelToken = new CancellationTokenSource();
                await WSC.ConnectAsync(Configurations.Uri, CancelToken.Token);
                if (IsRunning)
                {
                    await GetRelayCapabilities();

                    Receive();
                    Emit();

                    OnInitialConnectionEstablished?.Invoke(Configurations.Uri);
                }
                return IsRunning;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(Configurations.Uri, ex.Message);
                return IsRunning;
            }
        }
        /// <summary>
        /// Try to stop the ongoing Web Socket connection
        /// </summary>
        /// <returns>True if everything go smooth or if the connection was already stopped, false otherwise</returns>
        public async Task<bool> TryDisconnect()
        {
            if (!IsRunning)
                return true;

            try
            {
                if (CancelToken is null)
                    CancelToken = new CancellationTokenSource();
                await SendClose(new NRequestClose(SubscriptionId.ToString()), CancelToken.Token);
                await WSC.CloseAsync(WebSocketCloseStatus.NormalClosure, "stop requested", CancelToken.Token);
                CancelToken.Cancel();
                CancelToken.Dispose();
                return !IsRunning;
            }
            catch (Exception ex)
            {
                if (CancelToken is not null)
                {
                    CancelToken.Cancel();
                    CancelToken.Dispose();
                }
                OnError?.Invoke(Configurations.Uri, ex.Message);
                return !IsRunning;
            }
        }
        /// <summary>
        /// Perform a TryStop and a TryRun
        /// </summary>
        /// <returns>True if the Web Socket connection is now running, false otherwise</returns>
        public async Task<bool> TryReconnect()
        {
            try
            {
                if (await TryDisconnect())
                    await TryConnect();
                return IsRunning;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(Configurations.Uri, ex.Message);
                return IsRunning;
            }
        }


        /// <summary>
        /// Ask NIP-11 metadata to the relay
        /// </summary>
        /// <returns></returns>
        private async Task GetRelayCapabilities()
        {
            RelayNIP11Metadata? nip11RelayMetadata = await NSUtilities.GetNIP11RelayMetadata(Configurations.Uri);
            if (nip11RelayMetadata is not null)
            {
                Capabilities = nip11RelayMetadata;
                OnRelayMetadata?.Invoke(Configurations.Uri, nip11RelayMetadata);
            }
        }
        /// <summary>
        /// Set the read/write permissions for this relay connection instance
        /// </summary>
        /// <param name="permissions"></param>
        public void SetRelayPermissions(RelayPermissions permissions)
        {
            Configurations.RelayPermissions = permissions;
        }


        /// <summary>
        /// Try to send an Authentication request to the configured relay.
        /// NOTE: if this method return true it doesn't mean the authentication has been succesfully.
        /// To check if the authentication has been succesfully you need to subscribe to the event "OnAuthResponse".
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendAuthentication(NRequestAuth request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }
        /// <summary>
        /// Try to send an event to the configured relay.
        /// NOTE: if this method return true it doesn't mean the event has been accepted by the relay.
        /// To check if the event has been accepted, subscribe to "OnOk" event and check if the property
        /// "Accepted" is true and the property "EventId" is the same of the event you sent.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendEvent(NRequestEvent request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }
        /// <summary>
        /// Try to send a filter request to the configured relay.
        /// The result of the request can be seen by subscribing to the "OnEvent" event
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendFilter(NRequestReq request, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(request.SubscriptionId) || !Guid.TryParse(request.SubscriptionId, out _))
                request.SubscriptionId = SubscriptionId.ToString();
            return await Send(JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }
        /// <summary>
        /// Try to send a filter request to the configured relay.
        /// The result of the request can be seen by subscribing to the "OnCount" event
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendCount(NRequestCount request, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(request.SubscriptionId) || !Guid.TryParse(request.SubscriptionId, out _))
                request.SubscriptionId = SubscriptionId.ToString();
            return await Send(JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }
        /// <summary>
        /// Try to send a close subscription request.
        /// NOTE: this method doesn't close the Web Socket connection, but possibly the connection will be closed
        /// by the relay
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>True if the send has been succesful, false otherwise</returns>
        public async Task<bool> SendClose(NRequestClose request, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(request.SubscriptionId) || !Guid.TryParse(request.SubscriptionId, out _))
                request.SubscriptionId = SubscriptionId.ToString();
            return await Send(JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }


        public async Task<bool> Send(string msg, CancellationToken? token = null)
        {
            if (!IsRunning)
                return false;

            return await Send(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), token);
        }
        public async Task<bool> Send(byte[] msg, CancellationToken? token = null)
        {
            if (!IsRunning)
                return false;

            try
            {
                await WSC.SendAsync(msg, WebSocketMessageType.Text, true, token is null ? CancelToken.Token : (CancellationToken)token);
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(Configurations.Uri, ex.Message);
                return false;
            }
        }
        public async Task<bool> Send(ArraySegment<byte> msg, CancellationToken? token = null)
        {
            if (!IsRunning)
                return false;

            try
            {
                await WSC.SendAsync(msg, WebSocketMessageType.Text, true, token is null ? CancelToken.Token : (CancellationToken)token);
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(Configurations.Uri, ex.Message);
                return false;
            }
        }


        private async Task Receive()
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            while (IsRunning && !CancelToken.IsCancellationRequested)
            {

                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        WebSocketReceiveResult rm;
                        do
                        {
                            rm = await WSC.ReceiveAsync(buffer, CancelToken.Token);
                            if (rm.MessageType == WebSocketMessageType.Binary || buffer.Array is null)
                                continue;

                            ms.Write(buffer.Array, 0, Math.Min(rm.Count, buffer.Count));
                        }
                        while (!rm.EndOfMessage);
                        ms.Seek(0, SeekOrigin.Begin);


                        if (rm.MessageType == WebSocketMessageType.Close)
                            OnConnectionClosed?.Invoke(Configurations.Uri, rm.CloseStatusDescription ?? "");
                        else
                            _receivedData.Enqueue(ms.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(Configurations.Uri, ex.Message);
                }
            }
        }
        private async Task Emit()
        {
            while (IsRunning && !CancelToken.IsCancellationRequested)
                try
                {
                    if (!_receivedData.TryDequeue(out byte[]? receiveData) || receiveData is null)
                    {
                        await Task.Delay(200);
                        continue;
                    }

                    ParseMessagges(receiveData);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(Configurations.Uri, ex.Message);
                }
        }


        private void ParseMessagges(byte[] buffer)
        {
            string rawText = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            string msg = rawText.Trim();
            if (!msg.StartsWith("["))
                OnUnknownMessage?.Invoke(Configurations.Uri, new NResponseUnknown(Configurations.Uri, rawText, buffer, WebSocketMessageType.Text));
            else
            {
                string[] parts = msg.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts is null || parts.Length == 0 || string.IsNullOrEmpty(parts[0].ToString()))
                {
                    OnUnknownMessage?.Invoke(Configurations.Uri, new NResponseUnknown(Configurations.Uri, rawText, buffer, WebSocketMessageType.Text));
                    return;
                }

                string type = parts[0].ToString().ToUpperInvariant();
                if (type.Contains(NMessageTypes.Authentication))
                    OnAuthResponse?.Invoke(Configurations.Uri, ParseResponse<NResponseAuth>(msg));
                else if (type.Contains(NMessageTypes.Count))
                    OnCount?.Invoke(Configurations.Uri, ParseResponse<NResponseCount>(msg));
                else if (type.Contains(NMessageTypes.Event))
                    OnEvent?.Invoke(Configurations.Uri, ParseResponse<NResponseEvent>(msg));
                else if (type.Contains(NMessageTypes.Ok))
                    OnOk?.Invoke(Configurations.Uri, ParseResponse<NResponseOk>(msg));
                else if (type.Contains(NMessageTypes.Eose))
                    OnEose?.Invoke(Configurations.Uri, ParseResponse<NResponseEose>(msg));
                else if (type.Contains(NMessageTypes.Notice))
                    OnNotice?.Invoke(Configurations.Uri, ParseResponse<NResponseNotice>(msg));
                else
                    OnUnknownMessage?.Invoke(Configurations.Uri, new NResponseUnknown(Configurations.Uri, rawText, buffer, WebSocketMessageType.Text));
            }
        }
        private T ParseResponse<T>(string msg)
        {
            T? result = JsonConvert.DeserializeObject<T>(msg, SerializerCustomSettings.Settings);
            if (result is null)
                throw new ArgumentException("Cannot deserialize the response");
            return (T)result;
        }


        public async void Dispose()
        {
            await TryDisconnect();
            WSC.Dispose();
            Configurations = null;
        }
    }
}
