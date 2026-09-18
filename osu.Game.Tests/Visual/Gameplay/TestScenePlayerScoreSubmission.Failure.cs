// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Scoring;

namespace osu.Game.Tests.Visual.Gameplay
{
    public partial class TestScenePlayerScoreSubmission
    {
        [Test]
        public void TestRepeatedHealthFailureConcludesScoreOnce()
        {
            prepareTestAPI(true);
            createPlayerTest(true);
            AddUntilStep("wait for token request", () => Player.TokenCreationRequested);
            addFakeHit();
            AddUntilStep("first failure concluded", () => Player.GameplayState.HasFailed && Player.FailureConclusionCount == 1);
            AddUntilStep("first submission created", () => Player.SubmittedScore != null);

            AddStep("reapply pre-failure health judgement", () =>
            {
                var result = Player.Results.Last(result => !result.FailedAtJudgement);
                var health = Player.HealthProcessor;

                // Replay corrections can restore an earlier health state after the
                // player's failure animation and score finalisation have already begun.
                health.RevertResult(result);
                Assert.That(health.HasFailed, Is.False);
                health.ApplyResult(result);
                health.TriggerFailure();
                Assert.That(health.HasFailed, Is.True);
            });

            AddWaitStep("allow queued score finalisation", 3);
            AddAssert("player remains failed", () => Player.GameplayState.HasFailed);
            AddAssert("one failure transition", () => Player.FailureTransitionCount, () => Is.EqualTo(1));
            AddAssert("score concluded once", () => Player.FailureConclusionCount, () => Is.EqualTo(1));
            AddAssert("submission remains failed", () => Player.SubmittedScore.ScoreInfo.Passed, () => Is.False);
        }

        protected partial class FakeImportingPlayer
        {
            public int FailureTransitionCount { get; private set; }
            public int FailureConclusionCount { get; private set; }

            protected override void PerformFail()
            {
                FailureTransitionCount++;
                base.PerformFail();
            }

            protected override void ConcludeFailedScore(Score score)
            {
                FailureConclusionCount++;
                base.ConcludeFailedScore(score);
            }
        }
    }
}
