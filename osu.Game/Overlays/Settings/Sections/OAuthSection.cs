using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.General;
using osu.Game.Beatmaps;

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
                    Action = () => SpotifyManager.Init(),
                },
                new SettingsButton
                {
                    Text = new LocalisableString("OAuth"),
                    //TooltipText = GeneralSettingsStrings.LearnMoreAboutLazerTooltip,
                    BackgroundColour = colours.YellowDark,
                    //Action = () => game?.ShowWiki(@"Help_centre/Upgrading_to_lazer")
                },
            };
        }
    }
}
