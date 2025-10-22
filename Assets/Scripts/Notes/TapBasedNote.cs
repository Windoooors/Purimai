using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Notes
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

    public abstract class TapBasedNote : MonoBehaviour
    {
        protected const float TimeOnScreenWithBasicSpeed = 2.04166667f;
        public int timing;

        public SpriteRenderer lineSpriteRenderer;
        public Transform lineTransform;
        public int lane;
        public int indexInLane;

        public JudgeState judgeState;
        public bool isFast;

        [FormerlySerializedAs("judged")] public bool headJudged;

        private Animator _judgeDisplayAnimator;

        protected int EmergingDuration;
        protected float LineExpansionSpeed;

        protected float Speed;

        private void Start()
        {
            var laneIndex = lane - 1;
            var endPoint = Lanes.Instance.endPoints[laneIndex];
            var startPoint = Lanes.Instance.startPoints[laneIndex];

            var distance = (endPoint.position - startPoint.position).magnitude;
            var speed = distance / TimeOnScreenWithBasicSpeed * ChartPlayer.Instance.flowSpeed;

            LineExpansionSpeed = (1 - NoteGenerator.Instance.originCircleScale) / TimeOnScreenWithBasicSpeed *
                                 ChartPlayer.Instance.flowSpeed;

            var emergingDuration = TimeOnScreenWithBasicSpeed / ChartPlayer.Instance.flowSpeed;

            Speed = speed;
            EmergingDuration = (int)(emergingDuration * 1000);

            transform.position = Vector3.zero;
            transform.rotation = Lanes.Instance.startPoints[laneIndex].rotation;

            lineSpriteRenderer.color = new Color(1, 1, 1, 0);
            lineTransform.localScale = NoteGenerator.Instance.originCircleScale * Vector3.one;

            _judgeDisplayAnimator = TapJudgeDisplayManager.Instance.judgeDisplayAnimators[laneIndex];

            LateStart();
        }

        public virtual void RegisterTapEvent()
        {
        }

        protected void PlayJudgeAnimation()
        {
            transform.position = NoteGenerator.Instance.outOfScreenPosition;

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
        }

        protected virtual void LateStart()
        {
        }

        protected (JudgeState, bool isFast, bool judged) GetJudgeState(int deltaTiming, JudgeSettings judgeSettings)
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
    }
}