using EmbedIO.WebSockets;
using System;
using System.Threading.Tasks;
using osu.Framework.Logging;

namespace osu.Game.RemoteAudio
{
    public class ClientWebSocket : WebSocketModule
    {

        public ClientWebSocket() :
        base("/connect", true)
        {
            //AddProtocol("json"); // not sure what this does
            Logger.Log("created web socket");
        }

        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            string text = Encoding.GetString(rxBuffer);

            // process the web sockets here!!!!

            return SendAsync(context, "Message Recived");
            //return base.OnMessageReceivedAsync(context, rxBuffer, rxResult);
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Logger.Log("client connected???");
            return base.OnClientConnectedAsync(context);
        }

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            return base.OnClientDisconnectedAsync(context);
        }

        protected Task SendAll(string tag, string? data)
        {
            if (data == null)
                return BroadcastAsync(tag);
            return BroadcastAsync($"{tag}:{data}");
        }

        protected Task SendAll(string tag) => SendAll(tag, null);

        public Task PlayTrack(string uri)
        {
            // does this need to be convreted to json?
            Logger.Log("sending play request to ws");
            return SendAll("play", uri);
        }

        public Task Resume()
        {
            Logger.Log("sending resume to ws");
            return SendAll("resume");
        }

        public Task Pause()
        {
            Logger.Log("sending pause to ws");
            return SendAll("pause");
        }

        public Task SeekTo(long ms)
        {
            Logger.Log("sending seek to to ws");
            return SendAll("seek", ms.ToString());
        }

    }
}
