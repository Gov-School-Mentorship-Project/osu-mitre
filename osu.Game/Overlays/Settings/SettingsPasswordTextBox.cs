// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Overlays.Settings
{
    public partial class SettingsPasswordTextBox : SettingsItem<string>
    {
        /*protected override Drawable CreateControl() => new OutlinedTextBox
        {
            RelativeSizeAxes = Axes.X,
            CommitOnFocusLost = true
        };*/
        public string PlaceholderText { get; init; }

        protected override Drawable CreateControl() => new OsuPasswordTextBox
        {
            PlaceholderText = PlaceholderText,
            RelativeSizeAxes = Axes.X,
            CommitOnFocusLost = true
        };
                /*new OsuPasswordTextBox
                {
                    LabelText = "ClientId",
                    PlaceholderText = "TestPassword",
                    Padding = new MarginPadding { Left = SettingsPanel.CONTENT_MARGINS, Right = SettingsPanel.CONTENT_MARGINS  },
                    RelativeSizeAxes = Axes.X,
                    Current = clientSecret
                    //TabbableContentContainer = this,
                },*/


        public override Bindable<string> Current
        {
            get => base.Current;
            set
            {
                if (value.Default == null)
                    throw new InvalidOperationException($"Bindable settings of type {nameof(Bindable<string>)} should have a non-null default value.");

                base.Current = value;
            }
        }
    }
}
