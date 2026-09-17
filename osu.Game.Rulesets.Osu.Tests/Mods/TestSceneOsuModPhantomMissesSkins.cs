// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Timing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Configuration;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    // SkinnableTestScene supplies Argon, Triangles, default legacy, Retro, and two
    // custom legacy resource sets, including animated high-resolution skin assets.
    public partial class TestSceneOsuModPhantomMissesSkins : OsuSkinnableTestScene
    {
        private const double start_time = 1000;
        private readonly List<CirclePair> pairs = new List<CirclePair>();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [TestCase(-30, 0, 0.75, true, true)]
        [TestCase(0, 5, 1.0, true, true)]
        [TestCase(30, 10, 1.5, false, false)]
        [TestCase(0, 10, 1.0, true, false)]
        public void TestIdenticalMissTimingAndSkinTransforms(double hitOffset, float difficulty, double rate, bool animations, bool lighting)
        {
            createPairs(difficulty, rate, animations, lighting);
            seek(start_time + hitOffset);
            AddStep("hit phantom circles", () => pairs.ForEach(p => p.Phantom.TriggerHit()));
            AddWaitStep("apply all skin callbacks", 2);
            AddAssert("no premature miss or successful-hit skin state", () => pairs.All(p =>
                p.Phantom.Result.IsHit && p.Phantom.State.Value == ArmedState.Idle
                && p.PhantomJudgement == null && p.Phantom.SamplePlays == 0
                && !p.PhantomStates.Contains(ArmedState.Hit)));
            assertIdentical("unchanged before timeout");

            AddStep("seek to last hittable instant", () => pairs.ForEach(p => p.Seek(p.Deadline)));
            AddWaitStep("process inclusive hit window", 2);
            AddAssert("no miss at inclusive boundary", () => pairs.All(p => p.NativeJudgement == null && p.PhantomJudgement == null));
            assertIdentical("identical at hit-window boundary");

            AddStep("cross timeout", () => pairs.ForEach(p => p.Seek(p.Deadline + 0.01)));
            AddUntilStep("both misses displayed", () => pairs.All(p => p.NativeJudgement != null && p.PhantomJudgement != null));
            AddAssert("identical judgement time", () => pairs.All(p => p.NativeJudgement!.Result!.TimeAbsolute == p.PhantomJudgement!.Result!.TimeAbsolute));
            assertIdentical("identical miss frame");
            AddStep("advance miss fade", () => pairs.ForEach(p => p.Seek(p.Deadline + 50)));
            AddWaitStep("apply fade", 2);
            assertIdentical("identical mid-fade");
            AddStep("advance beyond circle fade", () => pairs.ForEach(p => p.Seek(p.Deadline + 101)));
            AddWaitStep("apply final fade", 2);
            AddAssert("real result and hit offset preserved", () => pairs.All(p =>
                p.Phantom.Result.Type == p.Phantom.HitObject.HitWindows!.ResultFor(hitOffset)
                && p.Phantom.Result.TimeOffset == hitOffset && p.PhantomResults == 1
                && p.PhantomDisplays == 1 && p.Native.Result.Type == HitResult.Miss));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestClassicAndHiddenSkinTransforms(bool hidden)
        {
            createPairs(5, 1, true, true, hidden ? new OsuModHidden() : new OsuModClassic());
            seek(start_time);
            AddStep("hit phantom circles", () => pairs.ForEach(p => p.Phantom.TriggerHit()));
            AddWaitStep("skin callbacks", 2);
            assertIdentical("identical after hidden/classic hit");
            AddStep("cross timeout", () => pairs.ForEach(p => p.Seek(p.Deadline + 1)));
            AddUntilStep("misses displayed", () => pairs.All(p => p.PhantomDisplays == 1));
            assertIdentical("identical hidden/classic miss");
        }

        [Test]
        public void TestPrematureGenuineMissUsesSamePresentation()
        {
            createPairs(5, 1, true, true);
            seek(start_time - 250);
            AddStep("miss early", () => pairs.ForEach(p =>
            {
                p.Presentation.IsPhantomTarget = false;
                p.Phantom.TriggerHit();
            }));
            AddWaitStep("process early miss", 2);
            AddAssert("real miss retained without early feedback", () => pairs.All(p =>
                p.Phantom.Result.Type == HitResult.Miss && p.Phantom.State.Value == ArmedState.Idle && p.PhantomDisplays == 0));
            assertIdentical("early genuine miss remains visually idle");
            AddStep("cross timeout", () => pairs.ForEach(p => p.Seek(p.Deadline + 1)));
            AddUntilStep("normalised genuine miss displayed", () => pairs.All(p => p.PhantomDisplays == 1));
            assertIdentical("genuine and phantom presentation equivalent");
        }

        [Test]
        public void TestRewindBetweenRealHitAndVisualMiss()
        {
            createPairs(5, 1, true, true);
            seek(start_time);
            AddStep("hit phantom circles", () => pairs.ForEach(p => p.Phantom.TriggerHit()));
            AddStep("cross timeout", () => pairs.ForEach(p => p.Seek(p.Deadline + 1)));
            AddUntilStep("miss displayed", () => pairs.All(p => p.PhantomDisplays == 1));
            seek(start_time + 10);
            AddAssert("rewind restores idle, retaining real hit", () => pairs.All(p =>
                p.Phantom.State.Value == ArmedState.Idle && p.Phantom.Result.IsHit && p.Presentation.Time == null));
            AddStep("cross timeout again", () => pairs.ForEach(p => p.Seek(p.Deadline + 1)));
            AddUntilStep("miss redisplayed once", () => pairs.All(p => p.PhantomDisplays == 2));
            AddAssert("rewind creates no extra scoring results", () => pairs.All(p => p.PhantomResults == 1));
        }

        [Test]
        public void TestReapplicationAndSkinRefreshRetainPresentation()
        {
            createPairs(5, 1, true, true);
            seek(start_time);
            AddStep("hit phantom circles", () => pairs.ForEach(p => p.Phantom.TriggerHit()));
            AddStep("cross timeout", () => pairs.ForEach(p => p.Seek(p.Deadline + 1)));
            AddUntilStep("miss displayed", () => pairs.All(p => p.PhantomDisplays == 1));
            AddStep("refresh skin state repeatedly", () => pairs.ForEach(p =>
            {
                p.Phantom.RefreshStateTransforms();
                p.Phantom.RefreshStateTransforms();
            }));
            AddWaitStep("process refreshed transforms", 2);
            assertIdentical("skin refresh keeps miss appearance");
            AddAssert("no duplicated feedback on refresh", () => pairs.All(p => p.PhantomDisplays == 1));
            AddStep("reuse drawable for non-target object", () => pairs.ForEach(p =>
            {
                p.Presentation.IsPhantomTarget = false;
                p.Phantom.Apply(createCircle(2000, 5));
                p.Seek(2000);
            }));
            AddWaitStep("apply reused drawable", 2);
            AddStep("hit non-target", () => pairs.ForEach(p => p.Phantom.TriggerHit()));
            AddAssert("normal hit presentation restored on reuse", () => pairs.All(p =>
                p.Phantom.State.Value == ArmedState.Hit && p.Phantom.Result.IsHit && p.Phantom.SamplePlays == 1));
        }

        private void createPairs(float difficulty, double rate, bool animations, bool lighting, osu.Game.Rulesets.Mods.IApplicableToDrawableHitObject? visualMod = null)
        {
            AddStep("create six skin comparisons", () =>
            {
                pairs.Clear();
                config.SetValue(OsuSetting.HitLighting, lighting);
                ((OsuRulesetConfigManager)RulesetConfigs.GetConfigFor(Ruleset.Value.CreateInstance())!).SetValue(OsuRulesetSetting.HitAnimations, animations);
                SetContents(_ =>
                {
                    var pair = new CirclePair(difficulty, rate);
                    visualMod?.ApplyToDrawableHitObject(pair.Native);
                    visualMod?.ApplyToDrawableHitObject(pair.Phantom);
                    pairs.Add(pair);
                    return pair;
                });
            });
            AddUntilStep("all six skin pairs loaded", () => pairs.Count == 6 && pairs.All(p => p.Native.IsLoaded && p.Phantom.IsLoaded));
        }

        private void seek(double time)
        {
            AddStep($"seek to {time}", () => pairs.ForEach(p => p.Seek(time)));
            AddWaitStep("process clock and transforms", 2);
        }

        private void assertIdentical(string description)
        {
            AddStep(description, () =>
            {
                foreach (var pair in pairs)
                {
                    Assert.That(pair.Phantom.Alpha, Is.EqualTo(pair.Native.Alpha).Within(0.00001));
                    Assert.That(pair.Phantom.HitStateUpdateTime, Is.EqualTo(pair.Native.HitStateUpdateTime));
                    compareDrawables(pair.Native.CirclePiece, pair.Phantom.CirclePiece);
                    compareDrawables(pair.Native.ApproachCircle, pair.Phantom.ApproachCircle);
                    if (pair.NativeJudgement != null && pair.PhantomJudgement != null)
                        compareDrawables(pair.NativeJudgement, pair.PhantomJudgement);
                }
            });
        }

        private static void compareDrawables(Drawable expected, Drawable actual)
        {
            var left = expected.ChildrenOfType<Drawable>().Prepend(expected).ToArray();
            var right = actual.ChildrenOfType<Drawable>().Prepend(actual).ToArray();
            Assert.That(right.Length, Is.EqualTo(left.Length), expected.GetType().Name);
            for (int i = 0; i < left.Length; i++)
            {
                Assert.That(right[i].GetType(), Is.EqualTo(left[i].GetType()));
                Assert.That(right[i].Alpha, Is.EqualTo(left[i].Alpha).Within(0.00001), left[i].GetType().Name);
                Assert.That(right[i].Scale, Is.EqualTo(left[i].Scale), left[i].GetType().Name);
                Assert.That(right[i].Size, Is.EqualTo(left[i].Size), left[i].GetType().Name);
                Assert.That(right[i].Rotation, Is.EqualTo(left[i].Rotation).Within(0.00001));
                Assert.That(right[i].Colour, Is.EqualTo(left[i].Colour), left[i].GetType().Name);
            }
        }

        private static HitCircle createCircle(double startTime, float difficulty)
        {
            var circle = new HitCircle { StartTime = startTime, Position = new Vector2(256, 192) };
            circle.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { OverallDifficulty = difficulty });
            return circle;
        }

        private partial class CirclePair : Container
        {
            public readonly TestCircle Native;
            public readonly TestCircle Phantom;
            public readonly CircleMissPresentation Presentation = new CircleMissPresentation { IsPhantomTarget = true };
            private readonly ManualClock manualClock;
            private readonly JudgementPooler<DrawableOsuJudgement> pool;
            private readonly JudgementContainer<DrawableOsuJudgement> judgements;
            public DrawableOsuJudgement? NativeJudgement;
            public DrawableOsuJudgement? PhantomJudgement;
            public readonly List<ArmedState> PhantomStates = new List<ArmedState>();
            public int PhantomResults;
            public int PhantomDisplays;
            public double Deadline => start_time + Native.HitObject.HitWindows!.WindowFor(HitResult.Meh);

            public CirclePair(float difficulty, double rate)
            {
                RelativeSizeAxes = Axes.Both;
                manualClock = new ManualClock { CurrentTime = start_time - 400, Rate = rate };
                Clock = new FramedClock(manualClock);
                var above = new Container { RelativeSizeAxes = Axes.Both };
                Children = new Drawable[]
                {
                    Native = new TestCircle(createCircle(start_time, difficulty)),
                    Phantom = new TestCircle(createCircle(start_time, difficulty)),
                    judgements = new JudgementContainer<DrawableOsuJudgement> { RelativeSizeAxes = Axes.Both },
                    above,
                    pool = new JudgementPooler<DrawableOsuJudgement>(new[] { HitResult.Miss }, j => above.Add(j.ProxiedAboveHitObjectsContent)),
                };
                Phantom.MissPresentationFor = _ => Presentation;
                Native.OnNewResult += (_, result) =>
                {
                    NativeJudgement = pool.Get(HitResult.Miss, j => j.Apply(result, Native));
                    judgements.Add(NativeJudgement!);
                };
                Phantom.ApplyCustomUpdateState += (_, state) => PhantomStates.Add(state);
                Phantom.OnNewResult += (_, _) => PhantomResults++;
                Phantom.DisplayMiss += (_, result) =>
                {
                    PhantomDisplays++;
                    PhantomJudgement = pool.Get(HitResult.Miss, j => j.Apply(result, Phantom));
                    judgements.Add(PhantomJudgement!);
                };
            }

            public void Seek(double time) => manualClock.CurrentTime = time;
        }

        private partial class TestCircle : DrawableHitCircle
        {
            public int SamplePlays;

            public TestCircle(HitCircle circle)
                : base(circle)
            {
                CheckHittable = (_, _, _) => ClickAction.Hit;
            }

            public void TriggerHit() => UpdateResult(true);

            public override void PlaySamples()
            {
                SamplePlays++;
                base.PlaySamples();
            }
        }
    }
}
