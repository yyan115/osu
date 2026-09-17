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
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    public partial class TestSceneOsuModPhantomMisses : OsuModTestScene
    {
        private const int object_count = 50;

        [Test]
        public void TestTargetSelectionRespectsWarmupAndSpacing()
        {
            const int circle_count = 2000;
            const int average_spacing = 180;
            const int warmup_circles = 40;
            const int minimum_gap = average_spacing / 2;
            const int maximum_gap = average_spacing * 3 / 2;

            for (int seed = 0; seed < 20; seed++)
            {
                int[] targets = OsuModPhantomMisses.SelectPhantomTargetIndices(circle_count, average_spacing, warmup_circles, seed).ToArray();

                Assert.That(targets, Is.Not.Empty);
                Assert.That(targets[0], Is.InRange(warmup_circles, warmup_circles + average_spacing - 1));

                for (int i = 1; i < targets.Length; i++)
                    Assert.That(targets[i] - targets[i - 1], Is.InRange(minimum_gap, maximum_gap));
            }
        }

        [Test]
        public void TestPhantomMissDoesNotChangeAuthoritativeJudgements()
        {
            bool sawPhantomMiss = false;

            CreateModTest(new ModTestData
            {
                Mod = new OsuModPhantomMisses
                {
                    Seed = { Value = 12345 },
                    AverageSpacing = { Value = 30 },
                    WarmupCircles = { Value = 0 },
                    MaskTargetHitsounds = { Value = false },
                    PlayComboBreakSound = { Value = false },
                    HideLiveScoreHud = { Value = false },
                    PreventFailure = { Value = false },
                },
                Autoplay = true,
                CreateBeatmap = createCircleBeatmap,
                PassCondition = () =>
                {
                    sawPhantomMiss |= Player.ChildrenOfType<DrawableOsuJudgement>()
                                              .Any(j => j.Result?.Type == HitResult.Miss);

                    return sawPhantomMiss
                           && !Player.HUDOverlay.ShowHud.Value
                           && Player.HUDOverlay.ShowHud.Disabled
                           && !Player.HUDOverlay.ShowHealthBar.Value
                           && Player.HUDOverlay.ShowHealthBar.Disabled
                           && Player.ScoreProcessor.JudgedHits >= object_count
                           && Player.Results.Count >= object_count
                           && Player.Results.All(result => result.Type == result.Judgement.MaxResult);
                }
            });
        }

        [Test]
        public void TestFailurePreventionCannotBeDisabled()
        {
            var mod = new OsuModPhantomMisses
            {
                PreventFailure = { Value = false },
            };

            Assert.That(mod.PerformFail(), Is.False);
            Assert.That(mod.RestartOnFail, Is.False);
        }

        [Test]
        public void TestDefaultConcealment()
        {
            bool sawPhantomMiss = false;

            CreateModTest(new ModTestData
            {
                Mod = new OsuModPhantomMisses
                {
                    Seed = { Value = 12345 },
                    AverageSpacing = { Value = 30 },
                    WarmupCircles = { Value = 0 },
                },
                Autoplay = true,
                CreateBeatmap = createCircleBeatmap,
                PassCondition = () =>
                {
                    sawPhantomMiss |= Player.ChildrenOfType<DrawableOsuJudgement>()
                                              .Any(j => j.Result?.Type == HitResult.Miss);

                    return sawPhantomMiss
                           && !Player.HUDOverlay.ShowHud.Value
                           && Player.HUDOverlay.ShowHud.Disabled
                           && !Player.HUDOverlay.ShowHealthBar.Value
                           && Player.HUDOverlay.ShowHealthBar.Disabled
                           && Player.Results.Count >= object_count
                           && Player.Results.All(result => result.Type == result.Judgement.MaxResult);
                }
            });
        }

        private static Beatmap createCircleBeatmap()
        {
            var hitObjects = new List<HitObject>(object_count);

            for (int i = 0; i < object_count; i++)
            {
                hitObjects.Add(new HitCircle
                {
                    Position = new Vector2(256, 192),
                    StartTime = 500 + i * 100,
                });
            }

            return new Beatmap
            {
                HitObjects = hitObjects,
            };
        }
    }
}
