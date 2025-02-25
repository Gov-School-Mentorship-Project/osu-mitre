using EmbedIO.WebSockets;
using System;
using System.Threading.Tasks;
using osu.Framework.Logging;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Allocation;
using System.Threading;
using ThrottleDebounce;
using System.Net;

namespace osu.Game.RemoteAudio
{
    public class ClientWebSocket : WebSocketModule
    {
        public NotificationOverlay? notifications;

        private RateLimitedAction<double> volumeDebouncer;

        public ClientWebSocket() :
        base("/connect", true)
        {
            //AddProtocol("json"); // not sure what this does
            Logger.Log("created web socket");

            Action<double> originalFunc = (double volume) => SendAll("volume", Math.Round(volume, 4).ToString());
            TimeSpan wait = TimeSpan.FromMilliseconds(100);
            volumeDebouncer = Debouncer.Debounce(originalFunc, wait, leading: false, trailing: true);
        }

        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            string text = Encoding.GetString(rxBuffer);

            // process the web sockets here!!!!
            String[] data = text.Split(":");
            switch (data[0])
            {
                case "token":
                    return SendAll("token", SpotifyManager.Instance.authentication?.Token?.Value?.AccessToken ?? String.Empty);
                case "device":
                    SpotifyManager.Instance.TransferDevice(data[1]);
                    return SendAll("device");
            }
            return SendAsync(context, "Message Received");
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Logger.Log("client connected???");
            if (SpotifyManager.Instance.LoggedIn)
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = "Webpage has successfully connected",
                    Icon = FontAwesome.Solid.Music,
                });
            }
            SpotifyManager.Instance.Volume();
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
            return SendAll("play", uri);
        }

        public Task Reset()
        {
            return SendAll("reset");
        }

        public Task Resume(int positionMs)
        {
            TimeSpan pos = TimeSpan.FromMilliseconds(positionMs);
            long timestamp = DateTimeOffset.Now.Subtract(pos).ToUnixTimeMilliseconds();
            Logger.Log($"should resume at {positionMs}");
            return SendAll("resume", timestamp.ToString());
        }

        public Task Pause()
        {
            return SendAll("pause");
        }

        public Task SeekTo(long ms)
        {
            return SendAll("seek", ms.ToString());
        }

        public void SetVolume(double volume)
        {
            volumeDebouncer.Invoke(volume);
        }
    }
}
