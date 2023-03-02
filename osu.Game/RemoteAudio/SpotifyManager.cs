using SpotifyAPI.Web;
using System.Collections.Generic;
using osu.Framework.Logging;
using EmbedIO;
using System;
using EmbedIO.Actions;
using osu.Game.Beatmaps.RemoteAudio;
using osu.Framework.Allocation;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays;
using osu.Game.Online.API;
using osu.Framework.Bindables;

namespace osu.Game.RemoteAudio
{
    public sealed class SpotifyManager // This should be a singleton to store all authorization and connections with the Spotify API
    {
        [Resolved]
        private INotificationOverlay? notifications { get; set; }
        private SpotifyClient? spotify;
        private WebServer server;
        public SpotifyTrack? currentTrack;
        public bool ready = false; // Has the web playback connected and has the device been transfered?
        public string? accessToken
        {
            get => _accessToken;
            set {
                if (value != null)
                {
                    _accessToken = value;
                    spotify = new SpotifyClient(value);
                }
            }
        }

        private string? _accessToken;
        public string? deviceId;

        private static readonly SpotifyManager instance;
        public static SpotifyManager Instance => instance;

        static SpotifyManager()
        {
            instance = new SpotifyManager();
        }

        private SpotifyManager() // Figure out when is the best time to create the spotify object... after OAuth?
        {
            server = RemoteAudioServerFactory.CreateSpotifyServer("http://localhost:9999");
            server.RunAsync();
            accessToken = "BQDlJu9dix6JeChGjj8Sh4qchOYgU91bYZ1WxXTvo6phr-UCZ4FxP8jNO_otcnijRbAblqv15Pr1plWYm56AHLxC7vlT8pMiVw-d3v1Y-_Buhe63LboJWdY_SRWBLoUrKhokB4hM4JjZCVsYemf-F4cdhNWy0mPxYfIuVTBlFV3AOxg4-VEACYSmnVkg58GJbLOMwyLkwuwctNvINqtGvsw4";
            Logger.Log($"Created Spotify Server with accessToken from SpotifyManager");
        }

        public static void Init()
        {
            Logger.Log($"Init SpotifyManager");
        }

        public void TransferDevice(string deviceId)
        {
            Logger.Log($"Trying to transfer device to {deviceId} from SpotifyManager");
            if (spotify == null)
                return;

            notifications?.Post(new SimpleNotification {Text = "Web SDK Has Connected. Now Transferring Spotify Playback Device."});
            spotify.Player.TransferPlayback(new PlayerTransferPlaybackRequest(new List<string> { deviceId }));
            ready = true;
        }

        public void Play(string reference) // Opens the provided input in Spotify. Checks if URL or URI is in valid format, but not if it links to an actual track
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            Logger.Log($"Playing {reference} from SpotifyManager");
            PlayerResumePlaybackRequest resume = new PlayerResumePlaybackRequest() { Uris = new List<string> { reference } };
            spotify.Player.ResumePlayback(resume);
        }

        public void Resume()
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            spotify.Player.ResumePlayback(new PlayerResumePlaybackRequest());
        }

        public void Stop()
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            Logger.Log($"Pausing track from SpotifyManager");
            spotify.Player.PausePlayback(new PlayerPausePlaybackRequest());
        }

        public void Seek(long ms)
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            Logger.Log($"Seeking to {ms} from SpotifyManager");
            spotify.Player.SeekTo(new PlayerSeekToRequest(Math.Clamp(ms, 0, (long)(currentTrack?.Length ?? 60000))));
        }

        public void OAuth(string clientId, string clientSecret)
        {
            OAuth authentication = new OAuth(clientId, clientSecret, "https://api.spotify.com/v1");
            Logger.Log($"Authorize with OAuth! id: {clientId}, secret: {clientSecret}");
            //authentication.TokenString = config.Get<string>(OsuSetting.Token);
            authentication.Token.ValueChanged += onTokenChanged;
            authentication.RequestAccessToken();
        }

        private void onTokenChanged(ValueChangedEvent<OAuthToken> e) {
            Logger.Log($"Got Access Token: {e.NewValue.AccessToken}");
            Instance.accessToken = e.NewValue.AccessToken;
            //config.SetValue(OsuSetting.Token, config.Get<bool>(OsuSetting.SavePassword) ? authentication.TokenString : string.Empty)
        }
    }
}
