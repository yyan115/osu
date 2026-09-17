// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Screens.Play
{
    public partial class HUDOverlay
    {
        /// <summary>
        /// Hides the separate playfield skin layer, which is not controlled by <see cref="ShowHud"/>.
        /// </summary>
        public void HidePlayfieldSkinLayer() => PlayfieldSkinLayer.Hide();
    }
}
