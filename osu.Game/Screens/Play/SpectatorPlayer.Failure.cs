// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Screens.Play
{
    public abstract partial class SpectatorPlayer
    {
        protected override void PerformFail()
        {
            // Replay corrections can rewind the health processor after the confirmed fail.
            // Keep the player transition terminal without repeating its animation or score finalisation.
            if (GameplayState.HasFailed)
                return;

            base.PerformFail();
        }
    }
}
