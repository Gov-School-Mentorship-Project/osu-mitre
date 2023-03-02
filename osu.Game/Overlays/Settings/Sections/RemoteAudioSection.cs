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
using osu.Framework.Bindables;
using osu.Game.Graphics.UserInterface;
using osu.Game.Configuration;
using osu.Game.Overlays.Settings.Sections.RemoteAudio;

namespace osu.Game.Overlays.Settings.Sections
{
    public partial class RemoteAudioSection : SettingsSection
    {
        public override LocalisableString Header => new LocalisableString("Remote Audio");
        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = FontAwesome.Solid.Music
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new SpotifySettings()
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
