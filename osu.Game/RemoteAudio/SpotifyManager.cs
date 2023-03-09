using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Game.Beatmaps.RemoteAudio;
using osu.Game.Configuration;
using osu.Game.Online.API;
using osu.Game.Online.API.OAuthMethods;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace osu.Game.RemoteAudio
{
    public sealed class SpotifyManager // This should be a singleton to store all authorization and connections with the Spotify API
    {
        public static SpotifyManager Instance { get; private set; }
        private OsuConfigManager? config;
        private INotificationOverlay? notification;

        // OAuth
        private WebServer server;
        public OAuthSpotify? authentication;
        public delegate void Notify(LoginState state, string? name);
        public event Notify? LoginStateUpdated;
        private string clientId = null!;
        private string clientSecret = null!;

        public bool LoggedIn => spotifyUsername != null;
        private SpotifyClient? spotify;
        private string? spotifyUsername = null;

        // Web SDK
        private ClientWebSocket socket;
        public SpotifyTrack? currentTrack;
        public bool ready = false; // Has the web playback connected and has the device been transfered?
        public string? deviceId;

        static SpotifyManager()
        {
            Instance = new SpotifyManager();
        }

        private SpotifyManager() // Figure out when is the best time to create the spotify object... after OAuth?
        {
            (server, socket) = RemoteAudioServerFactory.CreateSpotifyServer("http://localhost:9999");
            server.RunAsync();
            Logger.Log($"Created Spotify Server with accessToken from SpotifyManager");
            //LoginStateUpdated = new EventHandler<LoginStateUpdateEventArgs>();
        }

        public static void Init(INotificationOverlay notification, OsuConfigManager config)
        {
            Logger.Log($"Init SpotifyManager");
            //Instance = new SpotifyManager(notification, config);
            Instance.config = config;
            Instance.notification = notification;
            Instance.socket.notifications = notification;

            Instance.clientId = config.Get<string>(OsuSetting.RemoteAudioSpotifyClientId);
            Instance.clientSecret = config.Get<string>(OsuSetting.RemoteAudioSpotifyClientSecret);
            Instance.authentication = new OAuthSpotify(Instance.clientId, Instance.clientSecret, "https://api.spotify.com/v1");

            Instance.authentication.Token.ValueChanged += Instance.OnTokenChanged;
            Instance.authentication.Token.Value = OAuthToken.Parse(config.Get<string>(OsuSetting.SpotifyToken));
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
            if (!ready || spotify == null)
            {
                Logger.Log("Cannot Play Until Web device Is Registered");
                return;
            }

            Logger.Log($"Playing {reference} from SpotifyManager");
            PlayerResumePlaybackRequest resume = new PlayerResumePlaybackRequest() { Uris = new List<string> { reference } };
            spotify.Player.ResumePlayback(resume);
        }

        public void Resume()
        {
            if (!ready || spotify == null)
            {
                Logger.Log("Cannot Play Until Web Device Is Registered");
                return;
            }

            socket.Resume();
        }

        public void Stop()
        {
            if (!ready || spotify == null)
            {
                Logger.Log("Cannot Play Until Web Device Is Registered");
                return;
            }

            Logger.Log($"Pausing track from SpotifyManager");
            socket.Pause();
        }

        public void Seek(long ms)
        {
            if (!ready || spotify == null)
            {
                Logger.Log("Cannot Play Until Web Device Is Registered");
                return;
            }

            Logger.Log($"Seeking to {ms} from SpotifyManager");
            socket.SeekTo(ms);
        }

        public void Login(CancellationTokenSource cts)
        {
            Logger.Log($"Authorize with OAuth! id: {clientId}, secret: {clientSecret}");
            if (authentication == null)
                return;

            LoginStateUpdated?.Invoke(LoginState.Loading, string.Empty);

            authentication.AuthenticateWithPKCE(cts);
        }

        public void Logout()
        {
            //Instance.Token = null;
            authentication?.Clear();
        }

        public async Task<string?> GetName()
        {
            if (spotifyUsername != null)
                return spotifyUsername;

            if (spotify == null)
            {
                return null;
            }

            using var cts = new CancellationTokenSource();
            try
            {
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                return (await spotify.UserProfile.Current(cts.Token).ConfigureAwait(false)).DisplayName;
            }
            catch (TaskCanceledException) // Fetching the name took more than 30 seconds
            {
                Logger.Log("GetName() Request Timed Out");
                return null;
            }
            catch (Exception ex) when (ex is SpotifyAPI.Web.APIException || ex is System.Net.Http.HttpRequestException)
            {
                cts.Cancel();
                Logout();
                return null;
            }
        }

        private static async Task<string> WaitForCode(EmbedIOAuthServer server, CancellationToken cancellationToken)
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

        private async void OnTokenChanged(ValueChangedEvent<OAuthToken> e)
        {
            Logger.Log("OnTokenChanged() in SpotifyManager!");
            if (e.NewValue == null || e.NewValue.AccessToken == null)
            {
                spotify = null;
                spotifyUsername = null;
                //LoginStateUpdated?.Invoke(LoginState.LoggedOut, String.Empty);
                Logout();
                return;
            }

            PKCETokenResponse response = new PKCETokenResponse
            {
                AccessToken = e.NewValue.AccessToken,
                ExpiresIn = (int)e.NewValue.ExpiresIn,
                RefreshToken = e.NewValue.RefreshToken,
                TokenType = "Bearer",
                Scope = "playlist-read-private app-remote-control streaming user-modify-playback-state user-read-playback-state user-read-email user-read-private"
            };

            PKCEAuthenticator authenticator = new PKCEAuthenticator(clientId, response);

            /*authenticator.TokenRefreshed += (sender, response) =>
            {
                log.Debug("PKCE Token Refreshed!!!");
                SaveAuthorization(response);
            };*/

            SpotifyClientConfig clientConfig = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);

            spotify = new SpotifyClient(clientConfig);
            config?.SetValue(OsuSetting.SpotifyToken, e.NewValue.ToString());
            if (spotify != null)
            {
                // TODO: Make this set the button to "cancel" but then actually call the cancellation token if pressed
                LoginStateUpdated?.Invoke(LoginState.Loading, string.Empty);
                spotifyUsername = await GetName().ConfigureAwait(true);
                Logger.Log($"Logged in with spotify username {spotifyUsername}");

                LoginStateUpdated?.Invoke(LoginState.LoggedIn, spotifyUsername);

                if (spotifyUsername == null)
                {
                    spotify = null;
                    notification?.Post(new SimpleNotification
                    {
                        Text = "Error connecting to Spotify!",
                        Icon = FontAwesome.Solid.Music,
                    });
                }
                else
                {
                    notification?.Post(new SimpleNotification
                    {
                        Text = $"Successfully connected to Spotify account: {spotifyUsername}",
                        Icon = FontAwesome.Solid.Music,
                    });
                }
            }
            else
            {
                Logger.Log("logged out of spotify from OnTokenChanged()");
                LoginStateUpdated?.Invoke(LoginState.LoggedOut, string.Empty);
            }
        }
    }

    public enum LoginState
    {
        LoggedOut,
        Loading,
        LoggedIn
    }
}
