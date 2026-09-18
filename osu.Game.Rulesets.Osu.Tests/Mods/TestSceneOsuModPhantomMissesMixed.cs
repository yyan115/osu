// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    public partial class TestSceneOsuModPhantomMissesMixed : OsuModTestScene
    {
        [Test]
        public void TestMixedRealAndPhantomMissesPreserveScoringAndTiming()
        {
            const int count = 50;
            var actualMisses = new HashSet<int> { 5, 37 };
            var targets = OsuModPhantomMisses.SelectPhantomTargetIndices(count, 30, 0, 12345).ToHashSet();
            var expectedMissTimes = targets.Union(actualMisses).Select(timeFor).ToHashSet();
            var visibleMissTimes = new HashSet<double>();
            var frames = new List<ReplayFrame>();
            for (int i = 0; i < count; i++)
            {
                if (actualMisses.Contains(i))
                    continue;

                frames.Add(new OsuReplayFrame(timeFor(i), positionFor(i), OsuAction.LeftButton));
                frames.Add(new OsuReplayFrame(timeFor(i) + 1, positionFor(i)));
            }

            CreateModTest(new ModTestData
            {
                Mod = new OsuModPhantomMisses
                {
                    Seed = { Value = 12345 },
                    AverageSpacing = { Value = 30 },
                    WarmupCircles = { Value = 0 },
                },
                Autoplay = false,
                ReplayFrames = frames,
                CreateBeatmap = () => new Beatmap
                {
                    HitObjects = Enumerable.Range(0, count).Select(i => (HitObject)new HitCircle
                    {
                        StartTime = timeFor(i),
                        Position = positionFor(i),
                    }).ToList(),
                },
                PassCondition = () =>
                {
                    foreach (var layer in Player.ChildrenOfType<JudgementContainer<DrawableOsuJudgement>>())
                    {
                        foreach (var judgement in layer.Children)
                        {
                            if (!judgement.IsAlive || !judgement.IsPresent || judgement.Result?.Type != HitResult.Miss
                                || judgement.JudgedHitObject is not HitCircle circle)
                                continue;

                            Assert.That(judgement.Result.TimeAbsolute,
                                Is.GreaterThan(circle.StartTime + circle.HitWindows!.WindowFor(HitResult.Meh)));
                            visibleMissTimes.Add(circle.StartTime);
                        }
                    }

                    if (Player.Results.Count != count || !visibleMissTimes.SetEquals(expectedMissTimes))
                        return false;

                    Assert.That(Player.Results.Count(r => r.Type == HitResult.Miss), Is.EqualTo(actualMisses.Count));
                    Assert.That(Player.Results.Count(r => r.IsHit), Is.EqualTo(count - actualMisses.Count));
                    Assert.That(Player.ScoreProcessor.Combo.Value, Is.EqualTo(12));
                    Assert.That(Player.ScoreProcessor.HighestCombo.Value, Is.EqualTo(31));
                    Assert.That(Player.ScoreProcessor.Accuracy.Value, Is.EqualTo(0.96).Within(0.000001));
                    Assert.That(Player.Results.Where(r => targets.Contains((int)((r.HitObject.StartTime - 500) / 300)))
                                      .All(r => r.IsHit || actualMisses.Contains((int)((r.HitObject.StartTime - 500) / 300))), Is.True);
                    return true;
                }
            });
        }

        private static double timeFor(int index) => 500 + index * 300;

        private static Vector2 positionFor(int index) => new Vector2(100 + index % 4 * 100, 100 + index % 3 * 80);
    }
}
