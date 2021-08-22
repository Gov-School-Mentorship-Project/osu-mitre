// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Screens.Edit.Setup;

namespace osu.Game.Rulesets.Mania.Edit.Setup
{
    public class ManiaSetupSection : RulesetSetupSection
    {
        private LabelledSwitchButton specialStyle;

        public ManiaSetupSection()
            : base(new ManiaRuleset().RulesetInfo)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                specialStyle = new LabelledSwitchButton
                {
                    Label = "Use special (N+1) style",
                    Current = { Value = Beatmap.BeatmapInfo.SpecialStyle }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            specialStyle.Current.BindValueChanged(_ => updateBeatmap());
        }

        private void updateBeatmap()
        {
            Beatmap.BeatmapInfo.SpecialStyle = specialStyle.Current.Value;
        }
    }
}
