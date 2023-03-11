using EmbedIO.WebSockets;
using System;
using System.Threading.Tasks;
using osu.Framework.Logging;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Allocation;

namespace osu.Game.RemoteAudio
{
    public class ClientWebSocket : WebSocketModule
    {
        public INotificationOverlay? notifications;

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

            return SendAsync(context, "Message Received");
            //return base.OnMessageReceivedAsync(context, rxBuffer, rxResult);
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Logger.Log("client connected???");
            notifications?.Post(new SimpleNotification
            {
                Text = "Webpage has successfully connected",
                Icon = FontAwesome.Solid.Music,
            });
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

        public Task Reset()
        {
            Logger.Log("sending reset to ws");
            return SendAll("reset");
        }

        public Task Resume(int positionMs)
        {
            Logger.Log("sending resume to ws");
            TimeSpan pos = TimeSpan.FromMilliseconds(positionMs);
            long timestamp = DateTimeOffset.Now.Subtract(pos).ToUnixTimeMilliseconds();
            return SendAll("resume", timestamp.ToString());
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
