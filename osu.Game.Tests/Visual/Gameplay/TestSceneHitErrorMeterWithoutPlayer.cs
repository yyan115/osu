// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Screens.Play.HUD.HitErrorMeters;

namespace osu.Game.Tests.Visual.Gameplay
{
    [HeadlessTest]
    public partial class TestSceneHitErrorMeterWithoutPlayer : OsuTestScene
    {
        [Test]
        public void TestLoadWithoutScoreProcessor()
        {
            HitErrorMeter[] meters = null!;

            AddStep("load meters outside gameplay", () => Child = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = meters = new HitErrorMeter[]
                {
                    new BarHitErrorMeter(),
                    new LegacyBarHitErrorMeter(),
                    new ColourHitErrorMeter(),
                },
            });
            AddUntilStep("all meter implementations loaded", () => meters.All(meter => meter.IsLoaded));
            AddStep("clear meters", () =>
            {
                foreach (var meter in meters)
                    meter.Clear();
            });
            AddStep("dispose meters", () => Clear());
        }
    }
}
