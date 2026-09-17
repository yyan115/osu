// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Spectator;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tests.Visual.Multiplayer;
using osu.Game.Tests.Visual.OnlinePlay;
using osu.Game.Tests.Visual.Spectator;

namespace osu.Game.Tests.Visual.Gameplay
{
    [TestFixture]
    public partial class TestSceneSpectatorList : OsuTestScene
    {
        private int counter;

        [Test]
        public void TestBasics()
        {
            SpectatorList list = null!;
            Bindable<LocalUserPlayingState> playingState = new Bindable<LocalUserPlayingState>();
            GameplayState gameplayState = new GameplayState(new Beatmap(), new OsuRuleset(), healthProcessor: new OsuHealthProcessor(0), localUserPlayingState: playingState);
            TestSpectatorClient spectatorClient = new TestSpectatorClient();
            TestMultiplayerClient multiplayerClient = new TestMultiplayerClient(new TestRoomRequestsHandler());

            AddStep("create spectator list", () =>
            {
                Children = new Drawable[]
                {
                    spectatorClient,
                    multiplayerClient,
                    new DependencyProvidingContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        CachedDependencies =
                        [
                            (typeof(GameplayState), gameplayState),
                            (typeof(SpectatorClient), spectatorClient),
                            (typeof(MultiplayerClient), multiplayerClient),
                        ],
                        Child = list = new SpectatorList
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        }
                    }
                };
            });

            AddStep("start playing", () => playingState.Value = LocalUserPlayingState.Playing);

            AddRepeatStep("add a user", () =>
            {
                int id = Interlocked.Increment(ref counter);
                ((ISpectatorClient)spectatorClient).UserStartedWatching([
                    new SpectatorUser
                    {
                        OnlineID = id,
                        Username = $"User {id}"
                    }
                ]);
            }, 10);

            AddUntilStep("spectators visible during play", () => list.ChildrenOfType<FillFlowContainer>().First().Alpha == 1);

            AddRepeatStep("remove random user", () => ((ISpectatorClient)spectatorClient).UserEndedWatching(
                spectatorClient.WatchingUsers[RNG.Next(spectatorClient.WatchingUsers.Count)].OnlineID), 5);

            AddStep("change font to venera", () => list.HeaderFont.Value = Typeface.Venera);
            AddStep("change font to torus", () => list.HeaderFont.Value = Typeface.Torus);
            AddStep("change header colour", () => list.HeaderColour.Value = new Colour4(RNG.NextSingle(), RNG.NextSingle(), RNG.NextSingle(), 1));

            AddStep("enter break", () => playingState.Value = LocalUserPlayingState.Break);
            AddStep("stop playing", () => playingState.Value = LocalUserPlayingState.NotPlaying);
            AddUntilStep("spectators hidden outside play", () => list.ChildrenOfType<FillFlowContainer>().First().Alpha == 0);
        }

        [Test]
        public void TestWithoutGameplayState()
        {
            SpectatorList list = null!;
            var spectatorClient = new TestSpectatorClient();
            var multiplayerClient = new TestMultiplayerClient(new TestRoomRequestsHandler());

            AddStep("create list without gameplay state", () =>
            {
                Children = new Drawable[]
                {
                    spectatorClient,
                    multiplayerClient,
                    new DependencyProvidingContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        CachedDependencies =
                        [
                            (typeof(SpectatorClient), spectatorClient),
                            (typeof(MultiplayerClient), multiplayerClient),
                        ],
                        Child = list = new SpectatorList(),
                    }
                };
            });
            AddUntilStep("list loads without gameplay", () => list.IsLoaded);
            AddStep("add spectator outside gameplay", () => ((ISpectatorClient)spectatorClient).UserStartedWatching([
                new SpectatorUser
                {
                    OnlineID = 1,
                    Username = "Preview spectator",
                }
            ]));
            AddUntilStep("spectator state processed", () => spectatorClient.WatchingUsers.Count == 1);
            AddAssert("no gameplay-dependent spectator display", () => list.ChildrenOfType<FillFlowContainer>().First().Alpha == 0);
        }
    }
}
