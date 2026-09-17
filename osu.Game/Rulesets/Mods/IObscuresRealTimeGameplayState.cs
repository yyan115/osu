// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Mods
{
    /// <summary>
    /// Marks a mod which intentionally obscures authoritative real-time gameplay state such as combo or judgement outcome.
    /// Mods whose own presentation depends on that state should declare this interface as incompatible.
    /// </summary>
    public interface IObscuresRealTimeGameplayState
    {
    }
}
