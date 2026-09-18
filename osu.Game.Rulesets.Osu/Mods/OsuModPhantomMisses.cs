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
    public partial class OsuModPhantomMisses : Mod, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToDrawableHitObject, IApplicableToHUD, IApplicableToPlayer, IApplicableFailOverride, IHasSeed, IObscuresRealTimeGameplayState, IOverridesComboBreakAudio
    {
        public override string Name => "Phantom Misses";

        public override string Acronym => "PM";

        public override LocalisableString Description => "Was that miss real? Find out when the map ends.";

        public override ModType Type => ModType.Fun;

        public override bool Ranked => false;

        public override bool ValidForMultiplayer => false;

        public override Type[] IncompatibleMods => new[]
        {
            typeof(OsuModBubbles),
            typeof(OsuModFlashlight),
            typeof(OsuModNoScope),
            typeof(OsuModBloom),
            typeof(OsuModMuted),
            typeof(ModAdaptiveSpeed),
        };

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

        // Kept as a bindable for compatibility with existing local tests/settings. Its value is
        // intentionally ignored: a target hitsound would reveal that a displayed miss was phantom.
        public BindableBool MaskTargetHitsounds { get; } = new BindableBool(true);

        // Kept as a bindable for compatibility with existing local tests/settings. Its value is
        // intentionally ignored: disabling audio normalisation would let native combo-break sounds
        // reveal which displayed misses were genuine.
        public BindableBool PlayComboBreakSound { get; } = new BindableBool(true);

        // Kept as a bindable for compatibility with existing local tests/settings. Its value is
        // intentionally ignored: live score, combo and health state would reveal genuine misses.
        public BindableBool HideLiveScoreHud { get; } = new BindableBool(true);

        // Kept as a bindable for compatibility with existing local tests/settings. Its value is
        // intentionally ignored: failing early would reveal genuine misses before the results screen.
        public BindableBool PreventFailure { get; } = new BindableBool(true);

        private readonly HashSet<HitCircle> phantomTargets = new HashSet<HitCircle>();
        private readonly Dictionary<HitCircle, CircleMissPresentation> circlePresentations = new Dictionary<HitCircle, CircleMissPresentation>();

        private JudgementContainer<DrawableOsuJudgement> judgementLayer = null!;
        private Container judgementAboveHitObjectLayer = null!;
        private JudgementPooler<DrawableOsuJudgement> judgementPooler = null!;
        private SkinnableSound comboBreakSample = null!;
        private IFrameStableClock gameplayClock = null!;

        public void ApplyToDrawableHitObject(DrawableHitObject drawable)
        {
            if (drawable is not DrawableHitCircle circle)
                return;

            circle.MissPresentationFor = getCirclePresentation;
            circle.DisplayMiss -= displayCircleMiss;
            circle.DisplayMiss += displayCircleMiss;
        }

        private CircleMissPresentation getCirclePresentation(HitCircle circle)
        {
            if (!circlePresentations.TryGetValue(circle, out var presentation))
            {
                circlePresentations.Add(circle, presentation = new CircleMissPresentation
                {
                    IsPhantomTarget = phantomTargets.Contains(circle),
                });
            }

            return presentation;
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
        }

        public void ApplyToHUD(HUDOverlay overlay)
        {
            // A previous application (or another mod) may already have locked these bindables.
            // Unlock before assigning even an identical value, then lock again for this play.
            overlay.ShowHud.Disabled = false;
            overlay.ShowHud.Value = false;
            overlay.ShowHud.Disabled = true;
            overlay.ShowHealthBar.Disabled = false;
            overlay.ShowHealthBar.Value = false;
            overlay.ShowHealthBar.Disabled = true;

            // This layer is intentionally not controlled by ShowHud. Hide it explicitly so
            // custom skins cannot expose score/combo/accuracy state during phantom play.
            overlay.HidePlayfieldSkinLayer();
        }

        public void ApplyToPlayer(Player player)
        {
            player.BreakOverlay.Hide();
            (player as ReplayPlayer)?.ReplayOverlay.Hide();
        }

        public bool PerformFail() => false;

        public bool RestartOnFail => false;

        private void selectPhantomTargets(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            phantomTargets.Clear();
            circlePresentations.Clear();

            HitCircle[] circles = drawableRuleset.Beatmap.HitObjects.OfType<HitCircle>().ToArray();
            if (circles.Length == 0)
                return;

            Seed.Value ??= RNG.Next();

            foreach (int index in SelectPhantomTargetIndices(circles.Length, AverageSpacing.Value, WarmupCircles.Value, Seed.Value.Value))
            {
                HitCircle target = circles[index];
                phantomTargets.Add(target);
                target.Samples.Clear();
            }
        }

        internal static IEnumerable<int> SelectPhantomTargetIndices(int circleCount, int averageSpacing, int warmupCircles, int seed)
        {
            if (circleCount <= 0)
                yield break;

            var random = new Random(seed);
            int minimumGap = Math.Max(1, averageSpacing / 2);
            int maximumGap = Math.Max(minimumGap, averageSpacing * 3 / 2);

            // Warmup is already an initial delay. Randomise only the phase of the first phantom
            // inside one average-spacing window rather than forcing another full inter-phantom gap.
            int index = warmupCircles + random.Next(0, Math.Max(1, averageSpacing));

            while (index < circleCount)
            {
                yield return index;
                index += random.Next(minimumGap, maximumGap + 1);
            }
        }

        private void onNewResult(DrawableHitObject judgedObject, JudgementResult realResult)
        {
            // Circle miss feedback is emitted by the drawable only after its normal hit
            // window expires. This also normalises premature genuine misses.
            if (judgedObject is DrawableHitCircle circle && circle.UsesDeferredMiss)
                return;

            // Hidden slider ticks can still break combo. Keep their feedback while taking
            // all native combo audio out of the authoritative scoring path.
            if (realResult.Type == HitResult.Miss || (realResult.ComboAtJudgement > 0 && realResult.ComboAfterJudgement == 0))
                playComboBreak();

            if (judgedObject.DisplayResult && realResult.HasResult)
                displayJudgement(judgedObject, realResult);
        }

        private void displayCircleMiss(DrawableHitCircle circle, JudgementResult visualResult)
        {
            playComboBreak();
            displayJudgement(circle, visualResult);
        }

        private void playComboBreak()
        {
            if (!gameplayClock.IsRewinding && !gameplayClock.IsCatchingUp.Value && !gameplayClock.IsPaused.Value)
                comboBreakSample.Play();
        }

        private void displayJudgement(DrawableHitObject judgedObject, JudgementResult visualResult)
        {
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

        private void onJudgementLoaded(DrawableOsuJudgement judgement)
        {
            judgementAboveHitObjectLayer.Add(judgement.ProxiedAboveHitObjectsContent);
        }
    }
}
