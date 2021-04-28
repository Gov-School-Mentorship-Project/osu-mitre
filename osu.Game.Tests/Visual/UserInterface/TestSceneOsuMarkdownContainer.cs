// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics.Containers.Markdown;
using osu.Game.Overlays;

namespace osu.Game.Tests.Visual.UserInterface
{
    public class TestSceneOsuMarkdownContainer : OsuTestScene
    {
        private OsuMarkdownContainer markdownContainer;

        [Cached]
        private readonly OverlayColourProvider overlayColour = new OverlayColourProvider(OverlayColourScheme.Orange);

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Children = new Drawable[]
            {
                new Box
                {
                    Colour = overlayColour.Background5,
                    RelativeSizeAxes = Axes.Both,
                },
                new BasicScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(20),
                    Child = markdownContainer = new OsuMarkdownContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y
                    }
                }
            };
        });

        [Test]
        public void TestLink()
        {
            AddStep("Add Link", () =>
            {
                markdownContainer.Text = "[Welcome to osu!](https://osu.ppy.sh)";
            });
        }

        [Test]
        public void TestFencedCodeBlock()
        {
            AddStep("Add Code Block", () =>
            {
                markdownContainer.Text = @"```markdown
# Markdown code block

This is markdown code block.
```";
            });
        }

        [Test]
        public void TestSeparator()
        {
            AddStep("Add Seperator", () =>
            {
                markdownContainer.Text = @"Line above

---

Line below";
            });
        }

        [Test]
        public void TestQuote()
        {
            AddStep("Add quote", () =>
            {
                markdownContainer.Text =
                    @"> Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";
            });
        }

        [Test]
        public void TestTable()
        {
            AddStep("Add Table", () =>
            {
                markdownContainer.Text =
                    @"| Left Aligned  | Center Aligned | Right Aligned |
| :------------------- | :--------------------: | ---------------------:|
| Long Align Left Text | Long Align Center Text | Long Align Right Text |
| Align Left           |      Align Center      |           Align Right |
| Left                 |         Center         |                 Right |";
            });
        }
    }
}
