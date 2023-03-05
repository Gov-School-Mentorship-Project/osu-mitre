using SpotifyAPI.Web;
using System.Collections.Generic;
using osu.Framework.Logging;
using osu.Framework.Extensions;
using EmbedIO;
using System;
using EmbedIO.Actions;
using SpotifyAPI.Web.Auth;
using osu.Game.Beatmaps.RemoteAudio;
using osu.Framework.Allocation;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays;
using osu.Game.Online.API;
using osu.Game.Online.API.OAuthMethods;
using osu.Framework.Bindables;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Graphics.Sprites;

namespace osu.Game.RemoteAudio
{
    public sealed class SpotifyManager // This should be a singleton to store all authorization and connections with the Spotify API
    {
        private SpotifyClient? spotify;
        private WebServer server;
        private ClientWebSocket socket;
        public SpotifyTrack? currentTrack;
        public bool ready = false; // Has the web playback connected and has the device been transfered?

        public OAuthToken? Token
        {
            get => _Token;
            set
            {
                spotify = value != null ? new SpotifyClient(value.AccessToken) : null;
                _Token = value;
            }
        }
        private OAuthToken? _Token;
        public string? deviceId;

        private static readonly SpotifyManager instance;
        public static SpotifyManager Instance => instance;

        static SpotifyManager()
        {
            instance = new SpotifyManager();
        }

        private SpotifyManager() // Figure out when is the best time to create the spotify object... after OAuth?
        {
            (server, socket) = RemoteAudioServerFactory.CreateSpotifyServer("http://localhost:9999");
            server.RunAsync();
            Logger.Log($"Created Spotify Server with accessToken from SpotifyManager");
        }

        public static void Init(INotificationOverlay notification)
        {
            Logger.Log($"Init SpotifyManager");
            Instance.socket.notifications = notification;
        }

        public void TransferDevice(string deviceId)
        {
            Logger.Log($"Trying to transfer device to {deviceId} from SpotifyManager");
            if (spotify == null)
                return;

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
                Logger.Log("Cannot Play Until Web Device Is Registered");
                return;
            }

            socket.Resume();
        }

        public void Stop()
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Device Is Registered");
                return;
            }

            Logger.Log($"Pausing track from SpotifyManager");
            socket.Pause();
        }

        public void Seek(long ms)
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Device Is Registered");
                return;
            }

            Logger.Log($"Seeking to {ms} from SpotifyManager");
            socket.SeekTo(ms);
        }

        public void Connect(string clientId, string clientSecret, INotificationOverlay notifications)
        {
            OAuthSpotify authentication = new OAuthSpotify(clientId, clientSecret, "https://api.spotify.com/v1");
            Logger.Log($"Authorize with OAuth! id: {clientId}, secret: {clientSecret}");
            authentication.AuthenticateWithPKCE();

            //authentication.TokenString = config.Get<string>(OsuSetting.Token);
            // TODO: See how to store the authorization

            authentication.Token.ValueChanged += async (ValueChangedEvent<OAuthToken> e) => {
                Instance.Token = e.NewValue;
                if (spotify != null)
                {
                    string spotifyUsername = (await spotify.UserProfile.Current().ConfigureAwait(false)).DisplayName;
                    notifications.Post(new SimpleNotification
                    {
                        Text = $"Successfully connected to Spotify account: {spotifyUsername}",
                        Icon = FontAwesome.Solid.Music,
                    });
                }
            };
        }

        static async Task<string> WaitForCode(EmbedIOAuthServer server, CancellationToken cancellationToken)
        {
            /* used to listen for when the server's
            AuthorizationCodeReceived event is called */
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<object, AuthorizationCodeResponse, Task> handler = (object sender, AuthorizationCodeResponse response) =>
            {
                Logger.Log("Authorization Code Received");
                tcs.TrySetResult(response.Code);
                return Task.CompletedTask;
            };

            Func<object, string, string?, Task> errorHandler = (object sender, string error, string? state) =>
            {
                Logger.Log(error);
                return Task.CompletedTask;
            };

            server.AuthorizationCodeReceived += handler; // register the event listener
            server.ErrorReceived += errorHandler;

            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            string code = await tcs.Task.ConfigureAwait(false);
            //string code = tcs.Task.GetResultSafely(); // wait for the server to receive the code
            server.AuthorizationCodeReceived -= handler;
            return code;
        }

        private void OnTokenChanged(ValueChangedEvent<OAuthToken> e) {
            Logger.Log($"Got Access Token: {e.NewValue.AccessToken}");
            Instance.Token = e.NewValue;

            // TODO: Potentially store the refresh and access tokens in the config somewhere
            //config.SetValue(OsuSetting.Token, config.Get<bool>(OsuSetting.SavePassword) ? authentication.TokenString : string.Empty)
        }
    }
}
