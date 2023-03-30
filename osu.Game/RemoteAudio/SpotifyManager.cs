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
using System.Linq;
using osu.Framework.Audio;

namespace osu.Game.RemoteAudio
{
    public sealed class SpotifyManager // This should be a singleton to store all authorization and connections with the Spotify API
    {
        public static SpotifyManager Instance { get; private set; }
        private OsuConfigManager? config;
        private NotificationOverlay? notification;
        public AudioManager? audio;

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
        public SpotifyTrack? currentTrack; // TODO: Bring this out to RemoteAudioManager class
        public string? currentReference = null;

        public bool transferedDevice = false; // Has the web playback connected and has the device been transfered?
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

        public static void Init(NotificationOverlay notification, OsuConfigManager config, AudioManager audio)
        {
            Logger.Log($"Init SpotifyManager");
            //Instance = new SpotifyManager(notification, config);
            Instance.config = config;
            Instance.notification = notification;
            Instance.socket.notifications = notification;
            Instance.audio = audio;

            audio.VolumeTrack.BindValueChanged(Instance.VolumeChanged);
            audio.AggregateVolume.BindValueChanged(Instance.VolumeChanged);

            Instance.clientId = config.Get<string>(OsuSetting.RemoteAudioSpotifyClientId);
            Instance.clientSecret = config.Get<string>(OsuSetting.RemoteAudioSpotifyClientSecret);
            Instance.authentication = new OAuthSpotify(Instance.clientId, Instance.clientSecret, "https://api.spotify.com/v1");

            Instance.authentication.Token.ValueChanged += Instance.OnTokenChanged;
            Logger.Log($"Spotify Access Token {config.Get<string>(OsuSetting.SpotifyToken)}");
            Instance.authentication.Token.Value = OAuthToken.Parse(config.Get<string>(OsuSetting.SpotifyToken));
        }

        public void TransferDevice(string deviceId)
        {
            Logger.Log($"Trying to transfer device to {deviceId} from SpotifyManager");
            if (spotify == null)
                return;

            spotify.Player.TransferPlayback(new PlayerTransferPlaybackRequest(new List<string> { deviceId }));
            transferedDevice = true;
        }

        public void Play(string reference, int positionMs)
        {
            if (!transferedDevice || spotify == null)
            {
                Logger.Log("Cannot Play Until Web device Is Registered");
                return;
            }

            if (currentReference == reference)
            {
                Logger.Log("not playing becasue it is already playing ");
                Reset();
                return;
            } else
            {
                Logger.Log($"Changing from {currentReference} to {reference} at {positionMs}");
            }

            Logger.Log($"Playing {reference} from SpotifyManager");
            PlayerResumePlaybackRequest resume = new PlayerResumePlaybackRequest() {
                Uris = new List<string> { reference },
                PositionMs = positionMs,
            };
            currentReference = reference;
            spotify.Player.ResumePlayback(resume);
        }

        public void Reset()
        {
            if (!transferedDevice || spotify == null)
            {
                Logger.Log("Cannot Reset Until Web Device Is Registered");
                return;
            }

            socket.Reset();
        }

        public void Resume(int positionMs)
        {
            if (!transferedDevice || spotify == null)
            {
                Logger.Log("Cannot Resume Until Web Device Is Registered");
                return;
            }

            socket.Resume(positionMs);
        }

        public void Stop(RemoteTrack track)
        {
            if (!transferedDevice || spotify == null)
            {
                Logger.Log("Cannot Stop Until Web Device Is Registered");
                return;
            }

            if (track.reference != currentTrack?.reference) {
                Logger.Log("Not stopping track because it is not playing");
                return;
            }

            Logger.Log($"Pausing track from SpotifyManager");
            socket.Pause();
        }

        public void Seek(long ms)
        {
            if (!transferedDevice || spotify == null)
            {
                Logger.Log("Cannot Seek Until Web Device Is Registered");
                return;
            }

            Logger.Log($"Seeking to {ms} from SpotifyManager");
            socket.SeekTo(ms);
        }

        public void Volume(double volume)
        {
            if (!transferedDevice || spotify == null)
            {
                return;
            }

            socket.SetVolume(volume);
        }

        public void Volume()
        {
            if (Instance.audio != null)
                socket.SetVolume(Instance.audio.VolumeTrack.Value);
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
            catch (Exception e) when (e is SpotifyAPI.Web.APIException || e is System.Net.Http.HttpRequestException)
            {
                Logger.Log($"Error in Spotify Authorization: {e}");
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
                Logger.Log("trying to set new value to null");
                spotify = null;
                spotifyUsername = null;
                config?.SetValue<string?>(OsuSetting.SpotifyToken, null);
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

            authenticator.TokenRefreshed += (sender, response) =>
            {
                Logger.Log("PKCE Token Refreshed!!!");
                if (authentication == null)
                    return;

                authentication.Token.Value = new OAuthToken() {
                    AccessToken = response.AccessToken,
                    ExpiresIn = response.ExpiresIn,
                    RefreshToken = response.RefreshToken
                };
            };

            if (response.ExpiresIn < 0)
                return;

            SpotifyClientConfig clientConfig = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);

            spotify = new SpotifyClient(clientConfig);
            config?.SetValue(OsuSetting.SpotifyToken, e.NewValue.ToString());
            if (spotify != null)
            {
                // TODO: Make this set the button to "cancel" but then actually call the cancellation token if pressed
                LoginStateUpdated?.Invoke(LoginState.Loading, string.Empty);
                spotifyUsername = await GetName().ConfigureAwait(true);
                Logger.Log($"Logged in with spotify username {spotifyUsername}");


                if (spotifyUsername == null)
                {
                    LoginStateUpdated?.Invoke(LoginState.LoggedOut, string.Empty);
                    spotify = null;
                    notification?.Post(new SimpleErrorNotification
                    {
                        Text = "Error connecting to Spotify!",
                    });
                }
                else
                {
                    LoginStateUpdated?.Invoke(LoginState.LoggedIn, spotifyUsername);
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

        public void VolumeChanged(ValueChangedEvent<double> _)
        {
            Volume(audio?.AggregateVolume.Value * audio?.VolumeTrack.Value * currentTrack?.AggregateVolume.Value ?? 0);
        }

        public async Task<RemoteAudioInfo> GetRemoteBeatmapInfo(string reference)
        {
            if (spotify == null)
                return RemoteAudioInfo.DefaultError;

            string[] parts = reference.Split(":");
            string id = parts[parts.Length - 1];
            try
            {
                FullTrack track = await spotify.Tracks.Get(id).ConfigureAwait(false);
                TrackAudioFeatures features = await spotify.Tracks.GetAudioFeatures(id).ConfigureAwait(false);
                TrackAudioAnalysis analysis = await spotify.Tracks.GetAudioAnalysis(id).ConfigureAwait(false);

                List<Section> sections = analysis.Sections.Where(s => s.Tempo > 0 && s.TimeSignature > 0).Select(s => new Section(60000.0 / (double)s.Tempo, (double)s.Start * 1000.0, s.TimeSignature, 4)).ToList();
                if (sections.Count == 0)
                {
                    sections.Add(new Section(1000.0, 0.0, 4, 4));
                    sections.Add(new Section(1000, features.DurationMs - 1000, 4, 4));
                }

                IEnumerable<string> artistNames = from artist in track.Artists select artist.Name;

                return new RemoteAudioInfo(
                    string.Join(", ", artistNames),
                    track.Name,
                    features.DurationMs,
                    sections
                    );
            } catch (Exception e)
            {
                Logger.Log($"Error loading track info: {e}");
                return RemoteAudioInfo.DefaultError;
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
