// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Scoring;
using osu.Framework.Audio.Track;
using osu.Game.Beatmaps.RemoteAudio;
using System;

namespace osu.Game.Screens.Play.PlayerSettings
{
    public partial class RemoteAudioSettings : PlayerSettingsGroup
    {
        private readonly PlayerCheckbox useRemoteAudio;
        private bool? locked = null;
        private Action<bool> onChange;

        public RemoteAudioSettings(Track track, bool hasLocalAudio, Action<bool> onChange)
            : base("Remote Audio Settings")
        {
            this.onChange = onChange;
            Children = new Drawable[]
            {
                useRemoteAudio = new PlayerCheckbox { LabelText = "Use Remote Audio" },
            };
            bool hasRemoteAudio = typeof(RemoteTrack).IsAssignableFrom(track.GetType());
            if (hasRemoteAudio && !hasLocalAudio)
            {
               locked = true;
            }
            else if (!hasRemoteAudio && hasLocalAudio)
            {
                locked = false;
            }
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            useRemoteAudio.Current = config.GetBindable<bool>(OsuSetting.UseRemoteAudio);
            useRemoteAudio.Current.Disabled = false;
            if (locked != null)
            {
                config.SetValue<bool>(OsuSetting.UseRemoteAudio, locked ?? true);
            }
            else
            {
                useRemoteAudio.Current.BindValueChanged((ValueChangedEvent<bool> e) => onChange(e.NewValue));
            }
            useRemoteAudio.Current.Disabled = true;
            onChange(config.Get<bool>(OsuSetting.UseRemoteAudio));
        }
    }
}
