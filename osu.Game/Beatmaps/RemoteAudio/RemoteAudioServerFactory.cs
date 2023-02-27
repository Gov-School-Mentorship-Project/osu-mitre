using osu.Framework.Logging;
using EmbedIO;
using EmbedIO.Actions;
using System;


namespace osu.Game.Beatmaps
{
    public static class RemoteAudioServerFactory // TODO: Make this not a factory
    {
        public static WebServer CreateSpotifyServer(string url)
        {
            return new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                .WithCors()
                .WithModule(CreateStateHandler())
                .WithModule(CreateDeviceHandler())
                .WithModule(CreateAuthTokenHandler())
                .WithModule(CreateInteractionHandler()); // TODO: Remove this one here and in JS
        }
        static ActionModule CreateDeviceHandler()
        {
            return new ActionModule("/device", HttpVerbs.Post, (ctx) =>
            {
                string? deviceId = ctx.Request.QueryString[0];
                if (deviceId == null)
                    throw new HttpException(400);

                SpotifyManager.Instance.deviceId = deviceId;
                Logger.Log($"Device ID Received: {deviceId}");
                SpotifyManager.Instance.TransferDevice(deviceId);
                return ctx.SendDataAsync("");
            });
        }

        static ActionModule CreateAuthTokenHandler()
        {
            return new ActionModule("/token", HttpVerbs.Get, (ctx) =>
            {
                Logger.Log("Sending access token");
                if (SpotifyManager.Instance.accessToken == null)
                    throw new HttpException(404);

                return ctx.SendDataAsync(SpotifyManager.Instance.accessToken);
            });
        }

        static ActionModule CreateStateHandler()
        {
            return new ActionModule("/state", HttpVerbs.Post, (ctx) =>
            {
                var query = ctx.Request.QueryString;

                if (query[2] == null)
                    throw new HttpException(400);

                if (SpotifyManager.Instance.currentTrack == null)
                    throw new HttpException(404);

                long progress;
                long timestamp;
                if (long.TryParse(query[0], out progress))
                {
                    if (long.TryParse(query[1], out timestamp))
                    {
                        SpotifyManager.Instance.currentTrack.StateUpdate(timestamp, progress, query[2] == "true");
                    }
                }

                return ctx.SendDataAsync("");
            });
        }

        static ActionModule CreateInteractionHandler()
        {
            return new ActionModule("/interact", HttpVerbs.Post, (ctx) =>
            {
                return ctx.SendDataAsync("");
            });
        }
    }
}
