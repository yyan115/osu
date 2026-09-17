// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
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
                Assert.That(targets, Is.Unique);
                Assert.That(targets.All(i => i >= 0 && i < circle_count), Is.True);
                Assert.That(OsuModPhantomMisses.SelectPhantomTargetIndices(circle_count, average_spacing, warmup_circles, seed), Is.EqualTo(targets));

                for (int i = 1; i < targets.Length; i++)
                    Assert.That(targets[i] - targets[i - 1], Is.InRange(minimum_gap, maximum_gap));
            }
        }

        [TestCase(0, 0)]
        [TestCase(10, 10)]
        [TestCase(10, 40)]
        public void TestNoTargetsBeforeWarmup(int circleCount, int warmup)
        {
            Assert.That(OsuModPhantomMisses.SelectPhantomTargetIndices(circleCount, 30, warmup, 12345), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestPhantomMissDoesNotChangeAuthoritativeJudgements(bool legacyToggleValue)
        {
            var visibleMisses = new HashSet<HitObject>();
            var targetTimes = OsuModPhantomMisses.SelectPhantomTargetIndices(object_count, 30, 0, 12345)
                                               .Select(i => 500.0 + i * 100)
                                               .ToHashSet();

            CreateModTest(new ModTestData
            {
                Mod = new OsuModPhantomMisses
                {
                    Seed = { Value = 12345 },
                    AverageSpacing = { Value = 30 },
                    WarmupCircles = { Value = 0 },
                    MaskTargetHitsounds = { Value = legacyToggleValue },
                    PlayComboBreakSound = { Value = legacyToggleValue },
                    HideLiveScoreHud = { Value = legacyToggleValue },
                    PreventFailure = { Value = legacyToggleValue },
                },
                Autoplay = true,
                CreateBeatmap = createCircleBeatmap,
                PassCondition = () =>
                {
                    recordVisibleMisses(visibleMisses);

                    return visibleMisses.Count > 0
                           && visibleMisses.All(hitObject => targetTimes.Contains(hitObject.StartTime))
                           && !Player.HUDOverlay.ShowHud.Value
                           && Player.HUDOverlay.ShowHud.Disabled
                           && !Player.HUDOverlay.ShowHealthBar.Value
                           && Player.HUDOverlay.ShowHealthBar.Disabled
                           && Player.ScoreProcessor.JudgedHits == object_count
                           && Player.ScoreProcessor.Combo.Value == object_count
                           && Player.ScoreProcessor.Accuracy.Value == 1
                           && Player.Results.Count == object_count
                           && Player.Results.All(result => result.Type == result.Judgement.MaxResult)
                           && Player.Results.Where(result => targetTimes.Contains(result.HitObject.StartTime))
                                    .All(result => result.HitObject.Samples.Count == 0)
                           && Player.Results.Where(result => !targetTimes.Contains(result.HitObject.StartTime))
                                    .All(result => result.HitObject.Samples.Count > 0);
                }
            });
        }

        [Test]
        public void TestGenuineMissesRemainInAuthoritativeResults()
        {
            var visibleMisses = new HashSet<HitObject>();

            CreateModTest(new ModTestData
            {
                Mod = new OsuModPhantomMisses
                {
                    Seed = { Value = 12345 },
                    AverageSpacing = { Value = 30 },
                    WarmupCircles = { Value = 0 },
                },
                Autoplay = false,
                ReplayFrames = new List<ReplayFrame>(),
                CreateBeatmap = createCircleBeatmap,
                PassCondition = () =>
                {
                    recordVisibleMisses(visibleMisses);

                    return visibleMisses.Count > 0
                           && Player.Results.Count == object_count
                           && Player.Results.All(result => result.Type == HitResult.Miss)
                           && Player.ScoreProcessor.Combo.Value == 0
                           && Player.ScoreProcessor.Accuracy.Value == 0;
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
            Assert.That(mod.Ranked, Is.False);
            Assert.That(mod.ValidForMultiplayer, Is.False);
        }

        private void recordVisibleMisses(HashSet<HitObject> visibleMisses)
        {
            // Inspect only active judgement layers. The pool also contains preloaded MISS
            // placeholders, which must never count as evidence of a displayed phantom.
            foreach (var judgement in Player.ChildrenOfType<JudgementContainer<DrawableOsuJudgement>>()
                                            .SelectMany(layer => layer.Children))
            {
                if (judgement.IsAlive && judgement.IsPresent && judgement.Result?.Type == HitResult.Miss
                    && judgement.JudgedHitObject != null)
                {
                    visibleMisses.Add(judgement.JudgedHitObject);
                }
            }
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
                    Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) },
                });
            }

            return new Beatmap
            {
                HitObjects = hitObjects,
            };
        }
    }
}
