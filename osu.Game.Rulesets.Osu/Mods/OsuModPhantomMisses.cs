// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Audio;
using osu.Game.Configuration;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Osu.Mods
{
    /// <summary>
    /// Training mod which occasionally displays a synthetic miss for a hit circle that was
    /// actually hit. The authoritative judgement and score are never modified.
    /// </summary>
    public partial class OsuModPhantomMisses : Mod, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToHUD, IApplicableToPlayer, IApplicableFailOverride, IHasSeed, IReadFromConfig
    {
        public override string Name => "Phantom Misses";

        public override string Acronym => "PM";

        public override LocalisableString Description => "Was that miss real? Find out when the map ends.";

        public override ModType Type => ModType.Fun;

        public override bool Ranked => false;

        public override bool ValidForMultiplayer => false;

        public override Type[] IncompatibleMods => new[] { typeof(OsuModBubbles) };

        [SettingSource("Seed", "Use a custom seed instead of a random one", SettingControlType = typeof(SettingsNumberBox))]
        public Bindable<int?> Seed { get; } = new Bindable<int?>();

        [SettingSource("Average spacing", "Average number of hit circles between phantom misses.")]
        public BindableInt AverageSpacing { get; } = new BindableInt(180)
        {
            MinValue = 30,
            MaxValue = 600,
        };

        [SettingSource("Warmup circles", "Do not place a phantom miss before this many hit circles have passed.")]
        public BindableInt WarmupCircles { get; } = new BindableInt(40)
        {
            MinValue = 0,
            MaxValue = 500,
        };

        [SettingSource("Mask target hitsounds", "Silence phantom targets so a successful hit does not reveal itself through its hitsound.")]
        public BindableBool MaskTargetHitsounds { get; } = new BindableBool(true);

        [SettingSource("Play combo-break sound", "Give every displayed miss the same combo-break feedback so the sound cannot reveal whether it was real.")]
        public BindableBool PlayComboBreakSound { get; } = new BindableBool(true);

        [SettingSource("Hide live score HUD", "Hide score, accuracy, combo, health and other live HUD information that could reveal whether a miss was real.")]
        public BindableBool HideLiveScoreHud { get; } = new BindableBool(true);

        [SettingSource("Prevent failure", "Keep the play running so a real miss cannot reveal itself by ending the map early.")]
        public BindableBool PreventFailure { get; } = new BindableBool(true);

        private readonly HashSet<HitCircle> phantomTargets = new HashSet<HitCircle>();
        private readonly HashSet<JudgementResult> nativeComboBreakTriggers = new HashSet<JudgementResult>();
        private readonly Bindable<bool> alwaysPlayFirstComboBreak = new Bindable<bool>();

        private JudgementContainer<DrawableOsuJudgement> judgementLayer = null!;
        private Container judgementAboveHitObjectLayer = null!;
        private JudgementPooler<DrawableOsuJudgement> judgementPooler = null!;
        private SkinnableSound comboBreakSample = null!;
        private IFrameStableClock gameplayClock = null!;

        public void ReadFromConfig(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.AlwaysPlayFirstComboBreak, alwaysPlayFirstComboBreak);
        }

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            selectPhantomTargets(drawableRuleset);
            gameplayClock = drawableRuleset.FrameStableClock;

            // The built-in playfield would otherwise display the real hit judgement as well.
            // We replace the judgement presentation only; scoring still receives the original result.
            drawableRuleset.Playfield.DisplayJudgements.Value = false;

            var visualLayer = drawableRuleset.CreatePlayfieldAdjustmentContainer();
            visualLayer.Add(judgementLayer = new JudgementContainer<DrawableOsuJudgement>
            {
                RelativeSizeAxes = Axes.Both,
            });
            visualLayer.Add(judgementAboveHitObjectLayer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            drawableRuleset.Overlays.Add(visualLayer);

            drawableRuleset.Overlays.Add(judgementPooler = new JudgementPooler<DrawableOsuJudgement>(new[]
            {
                HitResult.Great,
                HitResult.Ok,
                HitResult.Meh,
                HitResult.Miss,
                HitResult.LargeTickHit,
                HitResult.SliderTailHit,
                HitResult.LargeTickMiss,
                HitResult.IgnoreMiss,
            }, onJudgementLoaded));

            drawableRuleset.Overlays.Add(comboBreakSample = new SkinnableSound(new SampleInfo("Gameplay/combobreak")));

            drawableRuleset.Playfield.NewResult += onNewResult;
            drawableRuleset.Playfield.RevertResult += onRevertResult;
        }

        public void ApplyToHUD(HUDOverlay overlay)
        {
            if (!HideLiveScoreHud.Value)
                return;

            // This is the same public HUD switch used by Cinema.
            overlay.ShowHud.Value = false;
            overlay.ShowHud.Disabled = true;
            overlay.ShowHealthBar.Value = false;
            overlay.ShowHealthBar.Disabled = true;
        }

        public void ApplyToPlayer(Player player)
        {
            if (!HideLiveScoreHud.Value)
                return;

            player.BreakOverlay.Hide();
            (player as ReplayPlayer)?.ReplayOverlay.Hide();
        }

        public bool PerformFail() => !PreventFailure.Value;

        public bool RestartOnFail => false;

        private void selectPhantomTargets(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            phantomTargets.Clear();
            nativeComboBreakTriggers.Clear();

            HitCircle[] circles = drawableRuleset.Beatmap.HitObjects.OfType<HitCircle>().ToArray();
            if (circles.Length == 0)
                return;

            Seed.Value ??= RNG.Next();
            var random = new Random(Seed.Value.Value);

            int averageSpacing = AverageSpacing.Value;
            int minimumGap = Math.Max(1, averageSpacing / 2);
            int maximumGap = Math.Max(minimumGap, averageSpacing * 3 / 2);
            int index = WarmupCircles.Value;

            while (true)
            {
                index += random.Next(minimumGap, maximumGap + 1);

                if (index >= circles.Length)
                    break;

                HitCircle target = circles[index];
                phantomTargets.Add(target);

                if (MaskTargetHitsounds.Value)
                    target.Samples.Clear();
            }
        }

        private void onNewResult(DrawableHitObject judgedObject, JudgementResult realResult)
        {
            // ComboEffects reacts to every combo reset, including results which are not visually
            // displayed. Track the exact set of results for which its native sound path triggers.
            bool comboActuallyReset = realResult.ComboAtJudgement > 0 && realResult.ComboAfterJudgement == 0;
            bool nativeComboBreakTriggered = comboActuallyReset
                                              && (realResult.ComboAtJudgement > 20
                                                  || (alwaysPlayFirstComboBreak.Value && nativeComboBreakTriggers.Count == 0));

            if (nativeComboBreakTriggered)
                nativeComboBreakTriggers.Add(realResult);

            if (!judgedObject.DisplayResult || !realResult.HasResult)
                return;

            JudgementResult visualResult = realResult;
            bool showPhantomMiss = realResult.IsHit
                                   && realResult.HitObject is HitCircle hitCircle
                                   && phantomTargets.Contains(hitCircle);

            if (showPhantomMiss)
            {
                // This result is used only by DrawableOsuJudgement. It is never submitted to
                // ScoreProcessor, HealthProcessor or GameplayState.
                var visualHitObject = new HitCircle
                {
                    StartTime = realResult.TimeAbsolute,
                };

                visualResult = new JudgementResult(visualHitObject, realResult.Judgement)
                {
                    Type = HitResult.Miss,
                };

                // A real miss fades a hit circle out over 100ms. Apply the same top-level
                // fade to a successfully hit phantom target to mask the most obvious
                // hit-vs-miss object animation difference without changing its judgement.
                if (judgedObject is DrawableHitCircle drawableHitCircle)
                    drawableHitCircle.FadeOut(100);
            }

            // Native ComboEffects only plays at >20 combo, or for the first combo break when
            // configured to do so. Fill in the other real misses ourselves so every displayed
            // Miss has the same audio cue as a phantom, without double-playing the native sound.
            bool canPlayGameplaySample = !gameplayClock.IsRewinding
                                         && !gameplayClock.IsCatchingUp.Value
                                         && !gameplayClock.IsPaused.Value;

            if (PlayComboBreakSound.Value
                && canPlayGameplaySample
                && (showPhantomMiss || (realResult.Type == HitResult.Miss && !nativeComboBreakTriggered)))
            {
                comboBreakSample.Play();
            }

            DrawableOsuJudgement? judgement = judgementPooler.Get(
                visualResult.Type,
                drawableJudgement => drawableJudgement.Apply(visualResult, judgedObject));

            if (judgement == null)
                return;

            judgementLayer.Add(judgement);

            judgementAboveHitObjectLayer.ChangeChildDepth(
                judgement.ProxiedAboveHitObjectsContent,
                (float)-visualResult.TimeAbsolute);
        }

        private void onRevertResult(JudgementResult result)
        {
            nativeComboBreakTriggers.Remove(result);
        }

        private void onJudgementLoaded(DrawableOsuJudgement judgement)
        {
            judgementAboveHitObjectLayer.Add(judgement.ProxiedAboveHitObjectsContent);
        }
    }
}
