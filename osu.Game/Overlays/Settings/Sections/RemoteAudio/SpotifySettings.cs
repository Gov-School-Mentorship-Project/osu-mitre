
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
        [Resolved(CanBeNull = true)]
        private OsuGame? game { get; set; }

        protected override LocalisableString Header => new LocalisableString("Spotify");

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Bindable<string> clientId = config.GetBindable<string>(OsuSetting.RemoteAudioSpotifyClientId);
            Bindable<string> clientSecret = config.GetBindable<string>(OsuSetting.RemoteAudioSpotifyClientSecret);

            Children = new Drawable[]
            {
                new SettingsButton
                {
                    Text = new LocalisableString("Open Webpage"),
                    Keywords = new[] { @"remote", @"audio", @"spotify" },
                    TooltipText = "Open web SDK in browser",
                    Action = OpenWebSDK
                },
                new SettingsButton
                {
                    Text = new LocalisableString("OAuth"),
                    TooltipText = "Connect with your Spotify account",
                    Action = () => SpotifyManager.Instance.OAuth(clientId.Value, clientSecret.Value),
                },
                new SettingsButton
                {
                    Text = new LocalisableString("Start Local Server"),
                    TooltipText = "Start local server to communicate with web SDK",
                    Action = () => SpotifyManager.Init(),
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

            string path = System.IO.Path.Combine(wd.FullName, "osu.Game/RemoteAudio/index.html");
            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            p.Start();
        }
    }
}
