// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Leaderboards;
using osu.Game.Scoring;
using osu.Game.Screens.Play.Leaderboards;
using osu.Game.Screens.Ranking;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Ranking
{
    public partial class TestSceneSoloResultsScreenLeaderboard
    {
        [Test]
        public void TestCachedScoresDoNotCompleteFreshLeaderboardFetch()
        {
            const long cached_score_id = 100001;
            const long fresh_score_id = 100002;
            var pendingRequests = new List<GetScoresRequest>();
            ScoreInfo localScore = null!;
            FetchObservingSoloResultsScreen screen = null!;

            AddStep("seed a complete cached leaderboard", () =>
            {
                localScore = TestResources.CreateTestScoreInfo(importedBeatmap);
                localScore.TotalScore = 151_000;
                localScore.OnlineID = -1;
                localScore.Position = null;
                localScore.User = API.LocalUser.Value;

                dummyAPI.HandleRequest = request =>
                {
                    if (request is not GetScoresRequest scoresRequest)
                        return false;

                    scoresRequest.TriggerSuccess(createResponse(cached_score_id, 1));
                    return true;
                };
                leaderboardManager.FetchWithCriteria(new LeaderboardCriteria(importedBeatmap, importedBeatmap.Ruleset, BeatmapLeaderboardScope.Global, null), forceRefresh: true);
            });
            AddUntilStep("cached scores ready", () => leaderboardManager.Scores.Value?.TopScores.Any(s => s.OnlineID == cached_score_id) == true);

            AddStep("hold fresh responses", () => dummyAPI.HandleRequest = request =>
            {
                if (request is not GetScoresRequest scoresRequest)
                    return false;

                pendingRequests.Add(scoresRequest);
                return true;
            });
            AddStep("show results", () => LoadScreen(screen = new FetchObservingSoloResultsScreen(localScore)));
            AddUntilStep("fresh request received", () => pendingRequests.Count > 0);
            AddWaitStep("allow cached-data continuations to run", 10);
            AddAssert("fetch still awaiting fresh data", () => !screen.ScoresAdded);
            AddAssert("cached score not displayed", () => this.ChildrenOfType<ScorePanel>().All(p => p.Score.OnlineID != cached_score_id));

            AddStep("return a partial fresh leaderboard", () => pendingRequests.Single().TriggerSuccess(createResponse(fresh_score_id, 200_000)));
            AddUntilStep("fresh scores added", () => screen.ScoresAdded);
            AddUntilStep("fresh score displayed", () => this.ChildrenOfType<ScorePanel>().Any(p => p.Score.OnlineID == fresh_score_id));
            AddAssert("cached score remains absent", () => this.ChildrenOfType<ScorePanel>().All(p => p.Score.OnlineID != cached_score_id));
            AddAssert("unknown local rank remains unknown", () => this.ChildrenOfType<ScorePanelList>().Single().GetPanelForScore(localScore).ScorePosition.Value, () => Is.Null);

            APIScoresCollection createResponse(long scoreID, int totalScores)
            {
                var score = TestResources.CreateTestScoreInfo(importedBeatmap);
                score.TotalScore = 300_000;
                score.OnlineID = scoreID;
                score.User = new APIUser { Id = 100003 };
                var soloScore = SoloScoreInfo.ForSubmission(score);
                soloScore.ID = (ulong)scoreID;
                return new APIScoresCollection
                {
                    Scores = new List<SoloScoreInfo> { soloScore },
                    ScoresCount = totalScores,
                };
            }
        }

        private partial class FetchObservingSoloResultsScreen : SoloResultsScreen
        {
            public bool ScoresAdded { get; private set; }

            public FetchObservingSoloResultsScreen(ScoreInfo score)
                : base(score)
            {
            }

            protected override void OnScoresAdded(ScoreInfo[] scores)
            {
                base.OnScoresAdded(scores);
                ScoresAdded = true;
            }
        }
    }
}
