﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Screens.Ranking;
using osu.Game.Users;
using osu.Game.Utils;
using osuTK.Input;
using osu.Framework.Logging;
using osu.Game.RemoteAudio;
using osu.Game.Configuration;

namespace osu.Game.Screens.Select
{
    public partial class PlaySongSelect : SongSelect
    {
        private OsuScreen? playerLoader;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        public override bool AllowExternalScreenChange => true;

        public override MenuItem[] CreateForwardNavigationMenuItemsForBeatmap(BeatmapInfo beatmap)
        {
            List<MenuItem> items = new List<MenuItem>()
            {
                new OsuMenuItem(ButtonSystemStrings.Edit.ToSentence(), MenuItemType.Standard, () => Edit(beatmap)),
            };

            if (RemoteBeatmapAudio.validateRemoteAudio(beatmap.Metadata.RemoteAudioReference))
            {
                items.Insert(0, new OsuMenuItem("Play with Remote Audio", MenuItemType.Highlighted, () => {
                    config.GetBindable<bool>(OsuSetting.UseRemoteAudio).Disabled = false;
                    config.SetValue<bool>(OsuSetting.UseRemoteAudio, true);
                    FinaliseSelection(beatmap);
                }));
            }

            if (beatmap.Metadata.AudioFile != string.Empty)
            {
                items.Insert(0, new OsuMenuItem("Play with Local Audio", MenuItemType.Highlighted, () =>
                {
                    Logger.Log("Playing with local audio");
                    config.GetBindable<bool>(OsuSetting.UseRemoteAudio).Disabled = false;
                    config.SetValue<bool>(OsuSetting.UseRemoteAudio, false);
                    FinaliseSelection(beatmap);
                }));
            }

            return items.ToArray();
        }


        protected override UserActivity InitialActivity => new UserActivity.ChoosingBeatmap();

        private PlayBeatmapDetailArea playBeatmapDetailArea = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            BeatmapOptions.AddButton(ButtonSystemStrings.Edit.ToSentence(), @"beatmap", FontAwesome.Solid.PencilAlt, colours.Yellow, () => Edit());
        }

        protected void PresentScore(ScoreInfo score) =>
            FinaliseSelection(score.BeatmapInfo, score.Ruleset, () => this.Push(new SoloResultsScreen(score, false)));

        protected override BeatmapDetailArea CreateBeatmapDetailArea()
        {
            playBeatmapDetailArea = new PlayBeatmapDetailArea
            {
                Leaderboard =
                {
                    ScoreSelected = PresentScore
                }
            };

            return playBeatmapDetailArea;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.KeypadEnter:
                    // this is a special hard-coded case; we can't rely on OnPressed (of SongSelect) as GlobalActionContainer is
                    // matching with exact modifier consideration (so Ctrl+Enter would be ignored).
                    FinaliseSelection();
                    return true;
            }

            return base.OnKeyDown(e);
        }

        private IReadOnlyList<Mod>? modsAtGameplayStart;

        private ModAutoplay? getAutoplayMod() => Ruleset.Value.CreateInstance().GetAutoplayMod();

        protected override bool OnStart()
        {
            if (playerLoader != null) return false;

            modsAtGameplayStart = Mods.Value;

            /*if (Beatmap.Value.Track is osu.Game.Beatmaps.RemoteAudio.RemoteTrack)
                Logger.Log("This is a remote track you're dealing with", level: LogLevel.Important);
            else
                osu.Framework.Logging.Logger.Log("THis is normal stuff here", level: LogLevel.Important);*/
            // Ctrl+Enter should start map with autoplay enabled.
            if (GetContainingInputManager().CurrentState?.Keyboard.ControlPressed == true)
            {
                var autoInstance = getAutoplayMod();

                if (autoInstance == null)
                {
                    notifications?.Post(new SimpleNotification
                    {
                        Text = NotificationsStrings.NoAutoplayMod
                    });
                    return false;
                }

                var mods = Mods.Value.Append(autoInstance).ToArray();

                if (!ModUtils.CheckCompatibleSet(mods, out var invalid))
                    mods = mods.Except(invalid).Append(autoInstance).ToArray();

                Mods.Value = mods;
            }

            SampleConfirm?.Play();

            this.Push(playerLoader = new PlayerLoader(createPlayer));
            return true;

            Player createPlayer()
            {
                Player player;

                var replayGeneratingMod = Mods.Value.OfType<ICreateReplayData>().FirstOrDefault();

                if (replayGeneratingMod != null)
                {
                    player = new ReplayPlayer((beatmap, mods) => replayGeneratingMod.CreateScoreFromReplayData(beatmap, mods))
                    {
                        LeaderboardScores = { BindTarget = playBeatmapDetailArea.Leaderboard.Scores }
                    };
                }
                else
                {
                    player = new SoloPlayer
                    {
                        LeaderboardScores = { BindTarget = playBeatmapDetailArea.Leaderboard.Scores }
                    };
                }

                return player;
            }
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);

            if (playerLoader != null)
            {
                Mods.Value = modsAtGameplayStart;
                playerLoader = null;
            }
        }
    }
}
