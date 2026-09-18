// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Taiko.Objects.Drawables;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Taiko.Tests
{
    /// <summary>
    /// Taiko doesn't output any samples. They are all handled externally by <see cref="DrumSamplePlayer"/>.
    /// </summary>
    [HeadlessTest]
    public partial class TestSceneSampleOutput : TestSceneTaikoPlayer
    {
        private readonly List<string> actualSampleNames = new List<string>();

        protected override TestPlayer CreatePlayer(Ruleset ruleset)
        {
            actualSampleNames.Clear();

            var player = base.CreatePlayer(ruleset);
            // Subscribe before the gameplay clock starts. Waiting for the player-loaded step
            // can already miss judgements on a fast headless run.
            player.OnLoadComplete += _ => player.DrawableRuleset.Playfield.NewResult += (dho, _) =>
            {
                if (dho is DrawableHit hit)
                    actualSampleNames.Add(string.Join(',', hit.GetSamples().Select(s => s.Name)));
            };
            return player;
        }

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            string[] expectedSampleNames =
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
            };

            AddUntilStep("all samples collected", () => actualSampleNames.Count == expectedSampleNames.Length);

            AddAssert("samples are correct", () => actualSampleNames, () => Is.EqualTo(expectedSampleNames));
        }

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TaikoBeatmapConversionTest().GetBeatmap("sample-to-type-conversions");
    }
}
