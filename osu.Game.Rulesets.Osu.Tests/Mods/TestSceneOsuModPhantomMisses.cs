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
                           && Player.ScoreProcessor.JudgedHits >= object_count
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
