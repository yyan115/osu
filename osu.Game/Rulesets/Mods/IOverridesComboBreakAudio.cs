// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Mods
{
    /// <summary>
    /// A mod which supplies its own combo-break audio independently of authoritative combo changes.
    /// </summary>
    public interface IOverridesComboBreakAudio : IApplicableMod
    {
    }
}
