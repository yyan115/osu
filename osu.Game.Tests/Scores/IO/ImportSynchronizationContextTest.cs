// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace osu.Game.Tests.Scores.IO
{
    public class ImportSynchronizationContextTest : ImportTest
    {
        [Test]
        public void TestSynchronousImportWorkerHasNoNotificationContext()
        {
            Assert.That(SynchronizationContext.Current, Is.Null);
        }

        [Test]
        public async Task TestAsyncImportContinuationHasNoNotificationContext()
        {
            Assert.That(SynchronizationContext.Current, Is.Null);

            await Task.Yield();

            Assert.That(SynchronizationContext.Current, Is.Null);
        }
    }
}
