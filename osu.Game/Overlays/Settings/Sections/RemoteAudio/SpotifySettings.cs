
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

namespace osu.Game.Overlays.Settings.Sections.RemoteAudio
{
    public partial class SpotifySettings : SettingsSubsection
    {
        //[Resolved(CanBeNull = true)]
        //private OsuGame? game { get; set; }
        private INotificationOverlay notificationOverlay = null!;
        protected override LocalisableString Header => new LocalisableString("Spotify");
        private SettingsButton? oauthButton;
        private SettingsButton? webpageButton;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, INotificationOverlay notifications)
        {
            Bindable<string> clientId = config.GetBindable<string>(OsuSetting.RemoteAudioSpotifyClientId);
            Bindable<string> clientSecret = config.GetBindable<string>(OsuSetting.RemoteAudioSpotifyClientSecret);
            notificationOverlay = notifications;

            Children = new Drawable[]
            {
                // TODO: Make it so that this button switches from a sign in to a sign out button
                oauthButton = new SettingsButton
                {
                    Text = new LocalisableString("Setup Spotify"),
                    TooltipText = "Connect with your Spotify account",
                    Action = () => {
                        SpotifyManager.Init(notificationOverlay);
                        SpotifyManager.Instance.Connect(clientId.Value, clientSecret.Value, notificationOverlay);
                        if (oauthButton != null)
                            oauthButton.Action = null;
                        if (webpageButton != null)
                        {
                            webpageButton.TooltipText = "Open spotify player in browser";
                            webpageButton.Action = OpenWebSDK;
                        }
                    }
                },
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
    }
}
