// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using NUnit.Framework;

namespace osu.Game.Tests
{
    public abstract partial class ImportTest
    {
        private SynchronizationContext? originalSynchronizationContext;

        [SetUp]
        public void SetUpImportSynchronizationContext()
        {
            originalSynchronizationContext = SynchronizationContext.Current;

            // Import tests access short-lived Realm instances from the test worker, while
            // the game runs on its own update thread. NUnit's SafeSynchronizationContext
            // posts to arbitrary thread-pool threads, not an owning-thread event loop.
            // Realm must not capture it as a thread-affine notification scheduler.
            // The game's independently installed update-thread context is unaffected.
            SynchronizationContext.SetSynchronizationContext(null);
        }

        [TearDown]
        public void TearDownImportSynchronizationContext()
        {
            SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
            originalSynchronizationContext = null;
        }
    }
}
