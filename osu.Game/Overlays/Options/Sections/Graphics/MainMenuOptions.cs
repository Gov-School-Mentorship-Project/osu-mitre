﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using osu.Framework.Allocation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Overlays.Options.Sections.Graphics
{
    public class MainMenuOptions : OptionsSubsection
    {
        protected override string Header => "Main Menu";

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new[]
            {
                new OsuCheckbox
                {
                    LabelText = "Snow",
                    Bindable = config.GetBindable<bool>(OsuConfig.MenuSnow)
                },
                new OsuCheckbox
                {
                    LabelText = "Parallax",
                    Bindable = config.GetBindable<bool>(OsuConfig.MenuParallax)
                },
                new OsuCheckbox
                {
                    LabelText = "Menu tips",
                    Bindable = config.GetBindable<bool>(OsuConfig.ShowMenuTips)
                },
            };
        }
    }
}