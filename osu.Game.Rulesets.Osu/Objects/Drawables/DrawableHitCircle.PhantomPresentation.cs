// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableHitCircle
    {
        // The provider is attached once per pooled drawable. Its state is keyed by HitObject,
        // so recycling a drawable or reloading a skin cannot change the recorded miss time.
        internal Func<HitCircle, CircleMissPresentation>? MissPresentationFor { get; set; }
        internal event Action<DrawableHitCircle, JudgementResult>? DisplayMiss;

        private HitCircle? presentationObject;
        private CircleMissPresentation? presentation;
        private bool missDisplayed;

        private CircleMissPresentation? circlePresentation
        {
            get
            {
                if (ParentHitObject != null || HitObject is not HitCircle circle || MissPresentationFor == null)
                    return null;

                if (presentationObject != circle)
                {
                    presentationObject = circle;
                    presentation = MissPresentationFor(circle);
                    missDisplayed = false;
                }

                return presentation;
            }
        }

        internal bool UsesDeferredMiss => circlePresentation != null && Result?.HasResult == true
                                          && (Result.Type == HitResult.Miss || circlePresentation.IsPhantomTarget);

        public override double HitStateUpdateTime => UsesDeferredMiss
            ? Math.Min(circlePresentation!.Time ?? HitObject.StartTime, HitObject.StartTime + HitObject.MaximumJudgementOffset)
            : base.HitStateUpdateTime;

        protected override ArmedState GetPresentationState(ArmedState requestedState)
        {
            if (requestedState == ArmedState.Idle)
            {
                if (circlePresentation != null)
                    circlePresentation.Time = null;

                missDisplayed = false;
                return ArmedState.Idle;
            }

            if (!UsesDeferredMiss)
                return base.GetPresentationState(requestedState);

            // Use the same strict hit-window test as CheckForResult(), on the same gameplay
            // clock. Do not use the 400ms MISS input window or a wall-clock scheduler.
            if (HitObject.HitWindows!.CanBeHit(Time.Current - HitObject.StartTime))
                return ArmedState.Idle;

            circlePresentation!.Time ??= Time.Current;
            return ArmedState.Miss;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (circlePresentation == null)
                return;

            if (!UsesDeferredMiss)
            {
                circlePresentation.Time = null;
                missDisplayed = false;
                return;
            }

            if (HitObject.HitWindows!.CanBeHit(Time.Current - HitObject.StartTime))
            {
                // A rewind can cross the presentation time without crossing the actual hit.
                // Restore the native idle appearance while retaining that successful result.
                circlePresentation.Time = null;
                missDisplayed = false;
                UpdateState(ArmedState.Idle);
                return;
            }

            UpdateState(ArmedState.Miss);

            if (missDisplayed || (Clock as IGameplayClock)?.IsRewinding == true)
                return;

            missDisplayed = true;

            // This result only reaches the visual judgement layer, never ScoreProcessor.
            // A distinct HitObject supplies the presentation time without changing RawTime
            // or TimeOffset on the authoritative result.
            DisplayMiss?.Invoke(this, new JudgementResult(new HitCircle
            {
                StartTime = HitStateUpdateTime,
            }, Result.Judgement)
            {
                Type = HitResult.Miss,
            });
        }

        protected override void OnApply()
        {
            base.OnApply();
            presentationObject = null;
            presentation = null;
            missDisplayed = false;
        }

        protected override void OnFree()
        {
            base.OnFree();
            presentationObject = null;
            presentation = null;
            missDisplayed = false;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            MissPresentationFor = null;
            DisplayMiss = null;
        }
    }
}
