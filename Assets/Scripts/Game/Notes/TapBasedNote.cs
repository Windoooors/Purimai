using System;
using Game.Notes.TapBasedNotes;
using UI.Result;
using UI.Settings;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Notes
{
    [Serializable]
    public class JudgeSettings
    {
        public int criticalPerfectTiming;
        public int semiCriticalPerfectTiming;
        public int perfectTiming;
        public int greatTiming;
        public int semiGreatTiming;
        public int quarterGreatTiming;
        public int lateGoodTiming;
        public int fastGoodTiming;
    }

    public enum JudgeState
    {
        CriticalPerfect,
        SemiCriticalPerfect,
        Perfect,
        Great,
        SemiGreat,
        QuarterGreat,
        Good,
        Miss
    }

    public abstract class TapBasedNote : NoteBase
    {
        public int timing;

        public SpriteRenderer lineSpriteRenderer;
        public Transform lineTransform;
        public int lane;
        public int indexInLane;

        public JudgeState judgeState;
        public bool isFast;

        [FormerlySerializedAs("judged")] public bool headJudged;

        private Animator _judgeDisplayAnimator;

        protected GameObject NoteContentRoot;
        protected Animator OffsetDisplayAnimator;

        protected int OnScreenTime;

        protected float Speed;

        private void Start()
        {
            var laneIndex = lane - 1;
            var endPoint = Lanes.Instance.endPoints[laneIndex];
            var startPoint = Lanes.Instance.startPoints[laneIndex];

            var onScreenTimeInSeconds = 4 / (((ChartPlayer.Instance.flowSpeed - 1) * 100 + 200) / 60);

            var distance = (endPoint.position - startPoint.position).magnitude;
            var speed = distance / onScreenTimeInSeconds;

            Speed = speed;
            OnScreenTime = (int)(onScreenTimeInSeconds * 1000);

            transform.position = Vector3.zero;
            transform.rotation = Lanes.Instance.startPoints[laneIndex].rotation;

            _judgeDisplayAnimator = JudgeDisplayManager.Instance.judgeDisplayAnimators[laneIndex];
            OffsetDisplayAnimator = JudgeDisplayManager.Instance.offsetDisplayAnimators[laneIndex];

            LateStart();

            NoteContentRoot = new GameObject("NoteContent");
            NoteContentRoot.transform.SetParent(transform);

            var children = transform.GetComponentsInChildren<Transform>();

            foreach (var child in children) child.parent = NoteContentRoot.transform;

            emergingTime = timing - OnScreenTime * 2;

            SetActive(false, NoteContentRoot);
        }

        public virtual void RegisterTapEvent()
        {
        }

        protected void PlayJudgeSound(bool isBreak, JudgeState state)
        {
            switch (state)
            {
                case JudgeState.CriticalPerfect:
                    if (isBreak)
                    {
                        SfxManager.Instance.PlayBreakCriticalPerfectSound();
                    }
                    else
                    {
                        SfxManager.Instance.PlayPerfectSound();
                    }

                    break;
                case JudgeState.Perfect:
                case JudgeState.SemiCriticalPerfect:
                    if (isBreak)
                        SfxManager.Instance.PlayBreakPerfectSound();
                    else
                        SfxManager.Instance.PlayPerfectSound();

                    break;
                case JudgeState.SemiGreat:
                case JudgeState.QuarterGreat:
                case JudgeState.Great:
                    if (isBreak)
                        SfxManager.Instance.PlayBreakGreatSound();
                    else
                        SfxManager.Instance.PlayGreatSound();

                    break;
                case JudgeState.Good:
                    if (isBreak)
                        SfxManager.Instance.PlayBreakGreatSound();
                    else
                        SfxManager.Instance.PlayGoodSound();

                    break;
            }
        }

        protected void PlayJudgeAnimation()
        {
            lineSpriteRenderer.enabled = false;

            if (judgeState is not JudgeState.CriticalPerfect and not JudgeState.Miss)
            {
                var settings = SettingsPool.GetValue("fast_late_display_level");

                switch (settings)
                {
                    case 0:
                        break;
                    case 1:
                        if (judgeState is not JudgeState.SemiCriticalPerfect and not JudgeState.Perfect)
                        {
                            OffsetDisplayAnimator.SetTrigger(isFast ? "ShowFast" : "ShowLate");
                            if (isFast)
                                Scoreboard.FastCount++;
                            else
                                Scoreboard.LateCount++;
                        }

                        break;
                    case 2:
                        OffsetDisplayAnimator.SetTrigger(isFast ? "ShowFast" : "ShowLate");
                        if (isFast)
                            Scoreboard.FastCount++;
                        else
                            Scoreboard.LateCount++;
                        break;
                }
            }

            if (this is Tap tap && tap.isBreak)
                switch (judgeState)
                {
                    case JudgeState.CriticalPerfect:
                        _judgeDisplayAnimator.SetTrigger("Show2600"); break;
                    case JudgeState.SemiCriticalPerfect:
                        _judgeDisplayAnimator.SetTrigger("Show2550"); break;
                    case JudgeState.Perfect:
                        _judgeDisplayAnimator.SetTrigger("Show2500"); break;
                    case JudgeState.Great:
                        _judgeDisplayAnimator.SetTrigger("Show2000"); break;
                    case JudgeState.SemiGreat:
                        _judgeDisplayAnimator.SetTrigger("Show1500"); break;
                    case JudgeState.QuarterGreat:
                        _judgeDisplayAnimator.SetTrigger("Show1250"); break;
                    case JudgeState.Good:
                        _judgeDisplayAnimator.SetTrigger("Show1000"); break;
                    case JudgeState.Miss:
                        _judgeDisplayAnimator.SetTrigger("ShowMiss"); break;
                }
            else
                switch (judgeState)
                {
                    case JudgeState.Perfect or JudgeState.CriticalPerfect or JudgeState.SemiCriticalPerfect:
                        _judgeDisplayAnimator.SetTrigger("ShowPerfect"); break;
                    case JudgeState.Great or JudgeState.SemiGreat or JudgeState.QuarterGreat:
                        _judgeDisplayAnimator.SetTrigger("ShowGreat"); break;
                    case JudgeState.Good:
                        _judgeDisplayAnimator.SetTrigger("ShowGood"); break;
                    case JudgeState.Miss:
                        _judgeDisplayAnimator.SetTrigger("ShowMiss"); break;
                }

            if (judgeState == JudgeState.Miss) return;
            AreaARipple.AreaARipples.Find(x => x.sensorId == "A" + lane).CancelAnimation();
            ChartPlayer.Instance.judgeCircleGlowAnimator.SetTrigger("ShowGlow");
        }

        protected virtual void LateStart()
        {
        }

        protected void GetTapOrLineTransform(ref TapOrLineTransform result)
        {
            if (result == null)
                return;

            var currentPosition = ChartPlayer.Instance.TimeInMilliseconds;

            var startEmergingTiming = timing - 2 * OnScreenTime;

            var startMovingTiming = timing - OnScreenTime;

            if (currentPosition < startEmergingTiming - 100 || currentPosition > timing + 200)
            {
                result.Shown = false;
                return;
            }

            if (currentPosition > startEmergingTiming && currentPosition < startMovingTiming)
            {
                var factor = 1 - (startMovingTiming - currentPosition) / OnScreenTime;

                result.Scale = factor * Vector3.one;
                result.Alpha = factor;
                result.PositionInLane = 0;
                result.Shown = true;

                return;
            }

            if (currentPosition >= startMovingTiming)
            {
                var factor = (currentPosition - startMovingTiming) / OnScreenTime;

                result.Scale = Vector3.one;
                result.Alpha = 1;
                result.PositionInLane = factor;
                result.Shown = true;

                return;
            }

            result.Scale = Vector3.zero;
            result.Alpha = 0;
            result.PositionInLane = 0;
            result.Shown = false;
        }

        protected (JudgeState, bool isFast, bool judged) GetJudgeState(float deltaTiming, JudgeSettings judgeSettings)
        {
            var absDeltaTiming = math.abs(deltaTiming);

            if (deltaTiming > judgeSettings.fastGoodTiming)
                return (JudgeState.Miss, false, false);

            if (deltaTiming < -judgeSettings.lateGoodTiming)
                return (JudgeState.Miss, false, false);

            var fast = deltaTiming > 0;

            var state = JudgeState.Miss;

            if ((absDeltaTiming <= judgeSettings.fastGoodTiming && absDeltaTiming > judgeSettings.quarterGreatTiming &&
                 fast)
                || (absDeltaTiming <= judgeSettings.lateGoodTiming &&
                    absDeltaTiming > judgeSettings.quarterGreatTiming && !fast))
                state = JudgeState.Good;
            if (absDeltaTiming <= judgeSettings.quarterGreatTiming && absDeltaTiming > judgeSettings.semiGreatTiming)
                state = JudgeState.QuarterGreat;
            if (absDeltaTiming <= judgeSettings.semiGreatTiming && absDeltaTiming > judgeSettings.greatTiming)
                state = JudgeState.SemiGreat;
            if (absDeltaTiming <= judgeSettings.greatTiming && absDeltaTiming > judgeSettings.perfectTiming)
                state = JudgeState.Great;
            if (absDeltaTiming <= judgeSettings.perfectTiming &&
                absDeltaTiming > judgeSettings.semiCriticalPerfectTiming)
                state = JudgeState.Perfect;
            if (absDeltaTiming <= judgeSettings.semiCriticalPerfectTiming &&
                absDeltaTiming > judgeSettings.criticalPerfectTiming)
                state = JudgeState.SemiCriticalPerfect;
            if (absDeltaTiming <= judgeSettings.criticalPerfectTiming)
                state = JudgeState.CriticalPerfect;

            return (state, fast, true);
        }

        protected class TapOrLineTransform
        {
            public float Alpha;
            public float PositionInLane;
            public Vector3 Scale;

            public bool Shown;
        }
    }
}