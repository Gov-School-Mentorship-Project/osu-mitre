
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.RemoteAudio;

namespace osu.Game.Overlays.Settings.Sections.RemoteAudio
{
    public partial class SpotifySettings : SettingsSubsection
    {
        protected override LocalisableString Header => new LocalisableString("Spotify");

        private RemoteAudioLoginButton? oauthButton;
        private SettingsButton? webpageButton;

        private Bindable<string> clientId = null!;
        private Bindable<string> clientSecret = null!;

        private CancellationTokenSource cts = null!;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, INotificationOverlay notifications)
        {
            SpotifyManager.Init(notifications, config);
            clientId = config.GetBindable<string>(OsuSetting.RemoteAudioSpotifyClientId);
            clientSecret = config.GetBindable<string>(OsuSetting.RemoteAudioSpotifyClientSecret);

            Children = new Drawable[]
            {
                oauthButton = new RemoteAudioLoginButton("Spotify"),
                webpageButton = new SettingsButton
                {
                    Text = new LocalisableString("Open Webpage"),
                    Keywords = new[] { @"remote", @"audio", @"spotify" },
                    TooltipText = "Open spotify player in browser",
                    Action = OpenWebSDK
                },
                new SettingsPasswordTextBox
                {
                    PlaceholderText = "Client Id",
                    TooltipText = "Client Id from Spotify Developer Dashboard",
                    LabelText = "Client Id",
                    Current = clientId
                },
                new SettingsPasswordTextBox
                {
                    PlaceholderText = "Client Secret",
                    TooltipText = "Client Secret from Spotify Developer Dashboard",
                    LabelText = "Client Secret",
                    Current = clientSecret
                }
            };

            oauthButton.SetState(LoginButtonState.Login, Logout);
            SpotifyManager.Instance.LoginStateUpdated += OnLoginStateUpdated;
        }

        private static void OpenWebSDK()
        {
            DirectoryInfo? wd = Directory.GetParent(Environment.CurrentDirectory);

            if (wd == null)
                return;

            string[] paths = new string[] { Directory.GetCurrentDirectory(), "osu.Game", "RemoteAudio", "index.html" };
            string path = Path.Combine(paths);

            Logger.Log(path);
            var p = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo(path)
                {
                    UseShellExecute = true
                }
            };
            p.Start();
        }

        private void Login()
        {
            cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            cts.Token.Register(() => oauthButton?.SetState(LoginButtonState.Login, Login));

            SpotifyManager.Instance.Login(cts);
            oauthButton?.SetState(LoginButtonState.Cancel, Cancel);
        }

        private void Logout()
        {
            if (oauthButton == null)
                return;

            SpotifyManager.Instance.Logout();
            oauthButton.SetState(LoginButtonState.Login, Login);
        }

        private void Cancel()
        {
            cts.Cancel();
        }

        public void OnLoginStateUpdated(LoginState state, string? username)
        {
            if (oauthButton == null)
                return;

            switch (state)
            {
                case LoginState.LoggedIn:
                    oauthButton.SetState(LoginButtonState.Logout, Logout, username ?? string.Empty);
                    break;
                case LoginState.Loading:
                    oauthButton.SetState(LoginButtonState.Cancel, Cancel);
                    break;
                case LoginState.LoggedOut:
                    oauthButton.SetState(LoginButtonState.Login, Logout);
                    break;
            }
        }
    }
}
