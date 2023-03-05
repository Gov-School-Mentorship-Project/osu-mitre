using SpotifyAPI.Web;
using System.Threading;
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
using osu.Framework.Bindables;
using System.Threading.Tasks;
using osu.Framework.Graphics.Sprites;

namespace osu.Game.RemoteAudio
{
    public sealed class SpotifyManager // This should be a singleton to store all authorization and connections with the Spotify API
    {
        [Resolved]
        private INotificationOverlay? notifications { get; set; }
        private SpotifyClient? spotify;
        private WebServer server;
        private ClientWebSocket socket;
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
            (server, socket) = RemoteAudioServerFactory.CreateSpotifyServer("http://localhost:9999");
            server.RunAsync();

            //accessToken = "BQDlJu9dix6JeChGjj8Sh4qchOYgU91bYZ1WxXTvo6phr-UCZ4FxP8jNO_otcnijRbAblqv15Pr1plWYm56AHLxC7vlT8pMiVw-d3v1Y-_Buhe63LboJWdY_SRWBLoUrKhokB4hM4JjZCVsYemf-F4cdhNWy0mPxYfIuVTBlFV3AOxg4-VEACYSmnVkg58GJbLOMwyLkwuwctNvINqtGvsw4";
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

            //spotify.Player.ResumePlayback(new PlayerResumePlaybackRequest());
            socket.Resume();
        }

        public void Stop()
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            Logger.Log($"Pausing track from SpotifyManager");
            //spotify.Player.PausePlayback(new PlayerPausePlaybackRequest());
            socket.Pause();
        }

        public void Seek(long ms)
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            Logger.Log($"Seeking to {ms} from SpotifyManager");
            //spotify.Player.SeekTo(new PlayerSeekToRequest(Math.Clamp(ms, 0, (long)(currentTrack?.Length ?? 60000))));
            socket.SeekTo(ms);
        }

        public async void OAuth(string clientId, string clientSecret, INotificationOverlay notifications)
        {
            // Osu system doesn't work without username + password
            /*OAuth authentication = new OAuth(clientId, clientSecret, "https://api.spotify.com/v1");
            Logger.Log($"Authorize with OAuth! id: {clientId}, secret: {clientSecret}");
            //authentication.TokenString = config.Get<string>(OsuSetting.Token);
            authentication.Token.ValueChanged += onTokenChanged;
            authentication.RequestAccessToken();*/

            Logger.Log("Generating Codes");
            (string verifier, string challenge) = PKCEUtil.GenerateCodes();
            Uri baseUri = new Uri("http://localhost:3000/callback");
            LoginRequest request = new LoginRequest(baseUri, clientId, LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = new List<string> {
                    Scopes.UserReadEmail,
                    Scopes.PlaylistReadPrivate,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadPlaybackState,
                    Scopes.Streaming,
                    Scopes.UserReadPrivate,
                }
            };

            Logger.Log("Creating oauth Server");
            EmbedIOAuthServer server = new EmbedIOAuthServer(baseUri, 3000);
            Logger.Log("Open oauth Browser");

            // open spotify OAuth to get permission from user
            BrowserUtil.Open(request.ToUri());
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

            Logger.Log("Starting oauth Server");
            server.Start().WaitSafely();

            // get the PKCE code from OAuth
            // TODO: Figure out why this isn't working
            string code = await WaitForCode(server, cts.Token).ConfigureAwait(true);
            Logger.Log("got code and now swapping for access token");

            // switch the PKCE code for an access token and refresh token
            PKCETokenRequest initialRequest = new PKCETokenRequest(clientId, code, baseUri, verifier);
            PKCETokenResponse initialResponse = await new OAuthClient().RequestToken(initialRequest).ConfigureAwait(false);

            accessToken = initialResponse.AccessToken;
            Logger.Log($"got access token: {initialResponse.AccessToken}");
            //SaveAuthorization(initialResponse);
            var authenticator = new PKCEAuthenticator(clientId, initialResponse);
            /*authenticator.TokenRefreshed += (sender, response) =>
            {
                Logger.Log("PKCE Token Refreshed!!!");
                SaveAuthorization(response);
            };*/
            spotify = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));

            Logger.Log("Stopping Server");
            await server.Stop().ConfigureAwait(false);
            Logger.Log("after stopping server!");

            string spotifyUsername = (await spotify.UserProfile.Current().ConfigureAwait(false)).DisplayName;
            notifications.Post(new SimpleNotification
            {
                Text = $"Successfully connected to Spotify account: {spotifyUsername}",
                Icon = FontAwesome.Solid.Music,
            });
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

        /*private void onTokenChanged(ValueChangedEvent<OAuthToken> e) {
            Logger.Log($"Got Access Token: {e.NewValue.AccessToken}");
            Instance.accessToken = e.NewValue.AccessToken;
            //config.SetValue(OsuSetting.Token, config.Get<bool>(OsuSetting.SavePassword) ? authentication.TokenString : string.Empty)
        }*/
    }
}
