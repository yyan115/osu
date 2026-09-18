// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Skinning;

namespace osu.Game.Tests.Visual.Gameplay
{
    public partial class TestScenePlayerSkinDependencies : TestSceneAllRulesetPlayers
    {
        private DependencyCheckingSkin skin = null!;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs<ISkinSource>(skin = new DependencyCheckingSkin(dependencies.Get<SkinManager>(), this));
            return dependencies;
        }

        protected override Player CreatePlayer(Ruleset ruleset)
        {
            skin.ResetChecks();
            return base.CreatePlayer(ruleset);
        }

        protected override void AddCheckSteps()
        {
            AddUntilStep("HUD skin was requested", () => skin.LookupCount > 0);
            AddAssert("HUD dependencies registered before skin loading", () => skin.MissingDependencyCount, () => Is.Zero);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            skin?.Dispose();
        }

        private class DependencyCheckingSkin : TrianglesSkin, ISkinSource
        {
            private readonly TestScenePlayerSkinDependencies owner;
            private int lookupCount;
            private int missingDependencyCount;

            public int LookupCount => Volatile.Read(ref lookupCount);
            public int MissingDependencyCount => Volatile.Read(ref missingDependencyCount);

            public DependencyCheckingSkin(IStorageResourceProvider resources, TestScenePlayerSkinDependencies owner)
                : base(resources)
            {
                this.owner = owner;
            }

            public void ResetChecks()
            {
                Volatile.Write(ref lookupCount, 0);
                Volatile.Write(ref missingDependencyCount, 0);
            }

            public override Drawable? GetDrawableComponent(ISkinComponentLookup lookup)
            {
                if (lookup is GlobalSkinnableContainerLookup { Lookup: GlobalSkinnableContainers.MainHUDComponents, Ruleset: null })
                {
                    var player = owner.Player;

                    if (player.Dependencies.Get<ScoreProcessor>() == null
                        || player.Dependencies.Get<HUDOverlay>() == null
                        || !ReferenceEquals(player.Dependencies.Get<HUDOverlay>(), ((TestPlayer)player).HUDOverlay))
                    {
                        Interlocked.Increment(ref missingDependencyCount);
                    }

                    Interlocked.Increment(ref lookupCount);
                }

                return base.GetDrawableComponent(lookup);
            }

            public event Action? SourceChanged
            {
                add { }
                remove { }
            }

            public ISkin FindProvider(Func<ISkin, bool> lookupFunction) => this;
            public IEnumerable<ISkin> AllSources => new[] { this };
        }
    }
}
