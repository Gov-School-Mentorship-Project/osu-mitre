// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Screens.Play.HUD
{
    /// <summary>
    /// Displays a single-line horizontal auto-sized flow of mods. For cases where wrapping is required, use <see cref="ModFlowDisplay"/> instead.
    /// </summary>
    public partial class ModDisplay : CompositeDrawable, IHasCurrentValue<IReadOnlyList<Mod>>
    {
        private const int fade_duration = 1000;

        public ExpansionMode ExpansionMode = ExpansionMode.ExpandOnHover;

        private readonly BindableWithCurrent<IReadOnlyList<Mod>> current = new BindableWithCurrent<IReadOnlyList<Mod>>();
        private List<Type> incompatible = new List<Type>();

        public Bindable<IReadOnlyList<Mod>> Current
        {
            get => current.Current;
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                current.Current = value;
            }
        }

        public List<Type> Incompatible
        {
            get => incompatible;
            set
            {
                if (value == null)
                {
                    incompatible = new List<Type>();
                    return;
                }

                if (value.Any(t => !typeof(Mod).IsAssignableFrom(t))) // Ensure that incompatable mod types are mods
                    throw new ArgumentException();

                incompatible = value;
            }
        }

        private readonly FillFlowContainer<ModIcon> iconsContainer;

        public ModDisplay()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = iconsContainer = new ReverseChildIDFillFlowContainer<ModIcon>
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Current.BindValueChanged(updateDisplay, true);

            iconsContainer.FadeInFromZero(fade_duration, Easing.OutQuint);
        }

        private void updateDisplay(ValueChangedEvent<IReadOnlyList<Mod>> mods)
        {
            iconsContainer.Clear();

            if (mods.NewValue == null) return;

            foreach (Mod mod in mods.NewValue)
            {
                osu.Framework.Logging.Logger.Log($"{((incompatible != null) ? incompatible.Count : false)} there is somethign ");
                if (incompatible != null && incompatible.Any(t => t.IsAssignableFrom(mod.GetType())))
                {
                    osu.Framework.Logging.Logger.Log($"got it :)");
                    iconsContainer.Add(new ModIcon(mod) { Scale = new Vector2(0.6f), Disabled = true});
                }
                else
                    iconsContainer.Add(new ModIcon(mod) { Scale = new Vector2(0.6f) });
            }

            appearTransform();
        }

        private void appearTransform()
        {
            expand();

            using (iconsContainer.BeginDelayedSequence(1200))
                contract();
        }

        private void expand()
        {
            if (ExpansionMode != ExpansionMode.AlwaysContracted)
                iconsContainer.TransformSpacingTo(new Vector2(5, 0), 500, Easing.OutQuint);
        }

        private void contract()
        {
            if (ExpansionMode != ExpansionMode.AlwaysExpanded)
                iconsContainer.TransformSpacingTo(new Vector2(-25, 0), 500, Easing.OutQuint);
        }

        protected override bool OnHover(HoverEvent e)
        {
            expand();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            contract();
            base.OnHoverLost(e);
        }
    }

    public enum ExpansionMode
    {
        /// <summary>
        /// The <see cref="ModDisplay"/> will expand only when hovered.
        /// </summary>
        ExpandOnHover,

        /// <summary>
        /// The <see cref="ModDisplay"/> will always be expanded.
        /// </summary>
        AlwaysExpanded,

        /// <summary>
        /// The <see cref="ModDisplay"/> will always be contracted.
        /// </summary>
        AlwaysContracted
    }
}
