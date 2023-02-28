using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.General;
using osu.Game.RemoteAudio;
using System;
using System.IO;

namespace osu.Game.Overlays.Settings.Sections
{
    public partial class OAuthSection : SettingsSection
    {
        [Resolved(CanBeNull = true)]
        private OsuGame? game { get; set; }


        //public override LocalisableString Header => OAuthSettingsStrings.OAuthSectionHeader;

        public override LocalisableString Header => new LocalisableString("Remote Audio");
        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = FontAwesome.Brands.Spotify
        };

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Children = new Drawable[]
            {
                new SettingsButton
                {
                    Text = new LocalisableString("Open Webpage"),
                    //Keywords = new[] { @"webpage", @"initial", @"getting started" },
                    //TooltipText = FirstRunSetupOverlayStrings.FirstRunSetupDescription,
                    Action = OpenWebSDK
                },
                new SettingsButton
                {
                    Text = new LocalisableString("OAuth"),
                    //TooltipText = GeneralSettingsStrings.LearnMoreAboutLazerTooltip,
                    BackgroundColour = colours.YellowDark,
                    Action = () => SpotifyManager.Init(),
                },
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
