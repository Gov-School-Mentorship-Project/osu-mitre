
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.RemoteAudio;
using System;
using System.IO;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using System.Threading;

namespace osu.Game.Overlays.Settings.Sections.RemoteAudio
{
    public partial class SpotifySettings : SettingsSubsection
    {
        private INotificationOverlay notificationOverlay = null!;
        protected override LocalisableString Header => new LocalisableString("Spotify");
        private RemoteAudioLoginButton? oauthButton;
        private SettingsButton? webpageButton;
        Bindable<string> clientId = null!;
        Bindable<string> clientSecret = null!;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, INotificationOverlay notifications)
        {
            clientId = config.GetBindable<string>(OsuSetting.RemoteAudioSpotifyClientId);
            clientSecret = config.GetBindable<string>(OsuSetting.RemoteAudioSpotifyClientSecret);
            notificationOverlay = notifications;

            Children = new Drawable[]
            {
                // TODO: Break this out into a separeate class SpotifyLoginButton : SettingsButton
                // to make it easier to manipulate the various states
                oauthButton = new RemoteAudioLoginButton("Spotify"),
                webpageButton = new SettingsButton
                {
                    Text = new LocalisableString("Open Webpage"),
                    Keywords = new[] { @"remote", @"audio", @"spotify" },
                    TooltipText = "Setup Spotify account before opening webpage",
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
            oauthButton.SetState(LoginButtonState.Login, Login);
        }

        static void OpenWebSDK()
        {
            DirectoryInfo? wd = Directory.GetParent(Environment.CurrentDirectory);

            if (wd == null)
                return;

            string[] paths = new String[] {System.IO.Directory.GetCurrentDirectory(), "osu.Game", "RemoteAudio", "index.html"};
            string path = System.IO.Path.Combine(paths);

            Logger.Log(path);
            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            p.Start();
        }

        void Login()
        {
            SpotifyManager.Init(notificationOverlay);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            cts.Token.Register(() => oauthButton?.SetState(LoginButtonState.Login, Login));

            SpotifyManager.Instance.Login(clientId.Value, clientSecret.Value, notificationOverlay, OnLoginComplete, cts);
            if (oauthButton != null)
            {
                oauthButton.SetState(LoginButtonState.Cancel, () =>
                {
                    cts.Cancel();
                });
            }
        }

        void Logout()
        {
            if (oauthButton == null)
                return;

            SpotifyManager.Instance.Logout();
            oauthButton.SetState(LoginButtonState.Login, Login);
        }

        void OnLoginComplete()
        {
            if (oauthButton == null)
                return;

            oauthButton.SetState(LoginButtonState.Logout, Logout);

            // TODO: Make the webpageButton able to be clicked any time and then the webpage able to handle it
            if (webpageButton != null)
            {
                Schedule( () =>
                {
                    webpageButton.TooltipText = "Open spotify player in browser";
                    webpageButton.Action = OpenWebSDK;
                });
            }
        }
    }
}
