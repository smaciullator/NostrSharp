using System;
using System.Net.WebSockets;

namespace NostrSharp.Relay.Models.Messagges
{
    public class NResponseUnknown
    {
        public Uri RelayUri { get; set; }
        public string Text { get; set; }
        public byte[] Binary { get; set; }
        public WebSocketMessageType MessageType { get; set; }


        public NResponseUnknown() { }
        public NResponseUnknown(Uri relayUri, string text, byte[] binary, WebSocketMessageType messageType)
        {
            RelayUri = relayUri;
            Text = text;
            Binary = binary;
            MessageType = messageType;
        }
    }
}
