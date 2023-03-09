using EmbedIO;
using EmbedIO.Actions;
using osu.Framework.Logging;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;

namespace osu.Game.RemoteAudio
{
    public static class RemoteAudioServerFactory // TODO: Make this not a factory
    {
        public static (WebServer, ClientWebSocket) CreateSpotifyServer(string url)
        {
            ClientWebSocket socket = new ClientWebSocket();
            WebServer server = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                .WithCors()
                .WithModule(CreateStateHandler())
                .WithModule(CreateDeviceHandler())
                .WithModule(CreateAuthTokenHandler())
                .WithModule(CreateInteractionHandler())
                .WithModule(socket);
            return (server, socket);
        }

        private static ActionModule CreateDeviceHandler()
        {
            return new ActionModule("/device", HttpVerbs.Post, (ctx) =>
            {
                string? deviceId = ctx.Request.QueryString[0];
                if (deviceId == null)
                    throw new HttpException(400);

                SpotifyManager.Instance.deviceId = deviceId;
                Logger.Log($"Device ID Received: {deviceId} from SpotifyServer");
                SpotifyManager.Instance.TransferDevice(deviceId);
                return ctx.SendDataAsync("");
            });
        }

        private static ActionModule CreateAuthTokenHandler()
        {
            return new ActionModule("/token", HttpVerbs.Get, (ctx) =>
            {
                //string? at = SpotifyManager.Instance.authentication?.RequestAccessToken() ?? null;
                string? accessToken = SpotifyManager.Instance.authentication?.Token?.Value?.AccessToken;
                if (accessToken == null)
                    throw new HttpException(404);

                Logger.Log($"Sending access token from SpotifyServer: {accessToken}");
                return ctx.SendDataAsync(accessToken);
            });
        }

        private static ActionModule CreateStateHandler()
        {
            return new ActionModule("/state", HttpVerbs.Post, (ctx) =>
            {
                Logger.Log("Received State From SpotifyServer");

                var query = ctx.Request.QueryString;
                if (query[2] == null)
                    throw new HttpException(400);

                if (SpotifyManager.Instance.currentTrack == null)
                    throw new HttpException(404);

                if (long.TryParse(query[0], out long progress))
                {
                    if (long.TryParse(query[1], out long timestamp))
                    {
                        SpotifyManager.Instance.currentTrack.StateUpdate(timestamp, progress, query[2] == "true");
                    }
                }

                return ctx.SendDataAsync("");
            });
        }

        private static ActionModule CreateInteractionHandler()
        {
            return new ActionModule("/interact", HttpVerbs.Post, (ctx) =>
            {
                return ctx.SendDataAsync("");
            });
        }
    }
}
