using Newtonsoft.Json;
using NostrSharp.Relay.Enums;
using NostrSharp.Relay.Models;
using NostrSharp.Relay.Models.Messagges;
using NostrSharp.Settings;
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


        public event EventHandler<Uri, NResponseAuth> OnAuthRequest;
        public event EventHandler<Uri, NResponseEvent> OnEvent;
        public event EventHandler<Uri, NResponseCount> OnCount;
        public event EventHandler<Uri, NResponseEose> OnEose;
        public event EventHandler<Uri, NResponseNotice> OnNotice;
        public event EventHandler<Uri, NResponseOk> OnOk;
        public event EventHandler<Uri, NResponseUnknown> OnUnknownMessage;
        public event EventHandler<Uri, string> OnError;
        #endregion


        public NSRelayConfig Configurations { get; private set; }
        public RelayNIP11Metadata Capabilities { get; private set; } = new();
        public string Name { get; set; }
        public bool IsRunning => WSC is not null ? WSC.State == WebSocketState.Open : false;


        private ClientWebSocket WSC { get; set; } = new ClientWebSocket();
        private CancellationToken cancellationToken { get; set; } = default;


        private ConcurrentQueue<byte[]> _receivedData = new();


        public NSRelay(NSRelayConfig config, CancellationToken cancellationToken = default)
        {
            if (config.Uri.ToString().EndsWith("/"))
                config.Uri = new Uri(config.Uri.ToString().Substring(0, config.Uri.ToString().Length - 1));

            Configurations = config;
            Name = config.Uri.Host;
            this.cancellationToken = cancellationToken;
        }


        public void SetCapabilities(RelayNIP11Metadata capabilities)
        {
            Capabilities = capabilities;
        }


        public async Task<bool> TryRun()
        {
            if (Configurations is null)
                return false;
            if (IsRunning)
                return true;

            try
            {
                await WSC.ConnectAsync(Configurations.Uri, cancellationToken);
                if (IsRunning)
                {
                    OnInitialConnectionEstablished?.Invoke(Configurations.Uri);
                    Receive();
                    Emit();
                }
                return IsRunning;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(Configurations.Uri, ex.Message);
                return IsRunning;
            }
        }
        public async Task<bool> TryStop()
        {
            if (!IsRunning)
                return true;

            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            try
            {
                await WSC.CloseAsync(WebSocketCloseStatus.NormalClosure, "stop requested", cancelTokenSource.Token);
                cancelTokenSource.Cancel();
                return !IsRunning;
            }
            catch (Exception ex)
            {
                cancelTokenSource.Cancel();
                OnError?.Invoke(Configurations.Uri, ex.Message);
                return !IsRunning;
            }
        }
        public async Task<bool> TryReconnect()
        {
            try
            {
                if (await TryStop())
                    await TryRun();
                return IsRunning;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(Configurations.Uri, ex.Message);
                return IsRunning;
            }
        }


        public async Task<bool> SendEvent(NRequestEvent request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }
        public async Task<bool> SendSubscriptioFilter(NRequestReq request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }
        public async Task<bool> SendClose(NRequestClose request, CancellationToken? token = null)
        {
            return await Send(JsonConvert.SerializeObject(request, SerializerCustomSettings.Settings), token);
        }
        public async Task<bool> Send(string msg, CancellationToken? token = null)
        {
            if (!IsRunning)
                return false;

            try
            {
                ArraySegment<byte> data = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
                await WSC.SendAsync(data, WebSocketMessageType.Text, true, token is null ? cancellationToken : (CancellationToken)token);
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(Configurations.Uri, ex.Message);
                return false;
            }
        }
        public async Task<bool> Send(byte[] msg, CancellationToken? token = null)
        {
            if (!IsRunning)
                return false;

            try
            {
                await WSC.SendAsync(msg, WebSocketMessageType.Text, true, token is null ? cancellationToken : (CancellationToken)token);
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
                await WSC.SendAsync(msg, WebSocketMessageType.Text, true, token is null ? cancellationToken : (CancellationToken)token);
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
            while (IsRunning && !cancellationToken.IsCancellationRequested)
            {

                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        WebSocketReceiveResult rm;
                        do
                        {
                            rm = await WSC.ReceiveAsync(buffer, cancellationToken);
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
            while (IsRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!_receivedData.TryDequeue(out byte[]? receiveData) || receiveData is null)
                    {
                        await Task.Delay(200);
                        //ThreadUtilities.PauseThread(100);
                        continue;
                    }

                    ParseMessagges(receiveData);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(Configurations.Uri, ex.Message);
                }
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
                    OnAuthRequest?.Invoke(Configurations.Uri, ParseResponse<NResponseAuth>(msg));
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
                throw new ArgumentException("Non è possibile deserializzare il messaggio");
            return (T)result;
        }


        public async void Dispose()
        {
            await TryStop();
            WSC.Dispose();
            Configurations = null;
        }
    }
}
