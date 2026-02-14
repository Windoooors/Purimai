using UI.Result;
using UI.Settings;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Notes.Taps
{
    public class Hold : TapBasedNote
    {
        public SpriteRenderer holdSpriteRenderer;
        public Transform holdTransform;
        public SpriteRenderer holdEndSpriteRenderer;
        public Transform holdEnd;

        public bool holdJudged;

        public int duration;

        public bool isHeadFast;

        private bool _compensationApplied;
        private int _disappearTime;
        private float _distance;

        private bool _emerging;

        private Animator _glowAnimator;
        private float _grossHoldSize;
        private JudgeState _headJudgeState;
        private bool _holdDone;

        private JudgeState _holdTailJudgeState;

        private HoldTransform _holdTransformData = new();
        private float _initialHoldLength;
        private bool _lineMoving;
        private bool _moving;

        private int _nowEmergingTimePosition;
        private TapOrLineTransform _tapOrLineTransform = new();

        private void Update()
        {
            if (!ChartPlayer.Instance.isPlaying || holdJudged)
                return;

            GetHoldTransform(ref _holdTransformData);

            if (!_holdTransformData.Shown)
            {
                NoteContentRoot.SetActive(false);
                return;
            }

            if (_holdTransformData.Shown && !headJudged && !holdJudged && !NoteContentRoot.activeSelf)
                NoteContentRoot.SetActive(true);

            if (ChartPlayer.Instance.GetTime() > timing + ChartPlayer.Instance.tapJudgeSettings.lateGoodTiming +
                ChartPlayer.Instance.judgeDelay &&
                !headJudged)
            {
                headJudged = true;
                holdJudged = true;
                judgeState = JudgeState.Miss;

                Scoreboard.HoldCount.Count(JudgeState.Miss);

                Scoreboard.ResetCombo();

                PlayJudgeAnimation();

                holdSpriteRenderer.enabled = false;
                holdEndSpriteRenderer.enabled = false;

                SimulatedSensor.OnTap -= JudgeHead;
                SimulatedSensor.OnLeave -= OnLeave;

                NoteContentRoot.SetActive(false);
            }

            if (ChartPlayer.Instance.GetTime() >
                timing + duration &&
                headJudged && !holdJudged)
                ChartPlayer.Instance.holdRippleAnimators[lane - 1].SetTrigger("Reset");

            if (ChartPlayer.Instance.GetTime() >
                timing + duration + ChartPlayer.Instance.holdTailJudgeSettings.greatTiming +
                ChartPlayer.Instance.judgeDelay &&
                headJudged && !holdJudged)
            {
                holdJudged = true;
                _holdTailJudgeState = JudgeState.Good;
                judgeState = JudgeState.Good;

                PlayJudgeSound(false, JudgeState.Good);
                PlayJudgeAnimation();

                holdSpriteRenderer.enabled = false;
                holdEndSpriteRenderer.enabled = false;

                _glowAnimator.SetTrigger("Reset");

                NoteContentRoot.SetActive(false);

                SimulatedSensor.OnLeave -= OnLeave;
            }

            holdTransform.position = Lanes.Instance.startPoints[lane - 1].position +
                                     (Lanes.Instance.endPoints[lane - 1].position -
                                      Lanes.Instance.startPoints[lane - 1].position) *
                                     _holdTransformData.PositionInLane;
            holdTransform.localScale = _holdTransformData.Scale;

            var color = new Color(1, 1, 1, _holdTransformData.Alpha);
            holdSpriteRenderer.color = color;
            lineSpriteRenderer.color = color;

            GetTapOrLineTransform(ref _tapOrLineTransform);

            lineTransform.localScale = (NoteGenerator.Instance.originCircleScale +
                                        (1 - NoteGenerator.Instance.originCircleScale) *
                                        (_holdTransformData.LinePositionInLane > 0
                                            ? _holdTransformData.LinePositionInLane
                                            : _tapOrLineTransform.PositionInLane))
                                       * Vector3.one;

            holdSpriteRenderer.size = new Vector2(holdSpriteRenderer.size.x, _holdTransformData.HoldSpriteLength);

            holdEndSpriteRenderer.enabled = _holdTransformData.TailDotShown;
            holdEndSpriteRenderer.transform.position = Lanes.Instance.startPoints[lane - 1].position +
                                                       (Lanes.Instance.endPoints[lane - 1].position -
                                                        Lanes.Instance.startPoints[lane - 1].position) *
                                                       _holdTransformData.TailDotPositionInLane;
        }

        private void GetHoldTransform(ref HoldTransform result)
        {
            var currentPosition = ChartPlayer.Instance.GetTime();

            var adjustedEmergingDuration = IsAdxFlowSpeedStyle ? OnScreenTime / 2 : OnScreenTime;

            var startEmergingTiming = timing - adjustedEmergingDuration - OnScreenTime;
            var startMovingTiming = timing - OnScreenTime;

            if (currentPosition < startEmergingTiming - 100 || currentPosition > timing + duration + 100)
            {
                result.Shown = false;
                return;
            }

            if (currentPosition > startEmergingTiming && currentPosition < startMovingTiming)
            {
                var factor = 1 - (startMovingTiming - currentPosition) / adjustedEmergingDuration;

                result.Scale = factor * Vector3.one;
                result.Alpha = factor;
                result.PositionInLane = 0;
                result.HoldSpriteLength = _initialHoldLength;
                result.Shown = true;
                result.TailDotShown = false;

                return;
            }

            result.Scale = Vector3.one;
            result.Alpha = 1;
            result.Shown = false;

            if (currentPosition >= startMovingTiming)
            {
                result.Shown = true;

                var totalHoldLength = duration * Speed / 1000f;
                var laneLength = (Lanes.Instance.endPoints[lane - 1].position -
                                  Lanes.Instance.startPoints[lane - 1].position).magnitude;

                if (currentPosition - startMovingTiming <= duration)
                {
                    result.HoldSpriteLength =
                        _initialHoldLength + totalHoldLength * (currentPosition - startMovingTiming) / duration;

                    if (result.HoldSpriteLength - _initialHoldLength > laneLength)
                        result.HoldSpriteLength = laneLength + _initialHoldLength;

                    result.TailDotShown = false;

                    result.PositionInLane = (result.HoldSpriteLength - _initialHoldLength) / 2 / laneLength;
                }

                if (currentPosition - startMovingTiming > duration && currentPosition < timing)
                {
                    result.TailDotShown = true;

                    var movableLength = laneLength - totalHoldLength;

                    var position = movableLength * ((currentPosition - startMovingTiming - duration) /
                                                    (OnScreenTime - duration)) + totalHoldLength / 2;

                    result.PositionInLane = position / laneLength;

                    result.HoldSpriteLength = totalHoldLength + _initialHoldLength;

                    result.TailDotPositionInLane = (currentPosition - startMovingTiming - duration) / OnScreenTime;
                }

                var startShrinkingTiming = timing + (totalHoldLength > laneLength ? duration - OnScreenTime : 0);

                if (currentPosition >= startShrinkingTiming)
                {
                    result.TailDotShown = true;

                    var maxHoldLength = (totalHoldLength > laneLength ? laneLength : totalHoldLength) +
                                        _initialHoldLength;

                    var position = laneLength - (maxHoldLength - _initialHoldLength) / 2 +
                                   (currentPosition - startShrinkingTiming) * Speed / 1000f / 2;

                    result.PositionInLane = position / laneLength;

                    result.HoldSpriteLength = maxHoldLength - (currentPosition - startShrinkingTiming) * Speed / 1000f;

                    result.TailDotPositionInLane =
                        (currentPosition - startShrinkingTiming + (duration > OnScreenTime
                            ? 0
                            : OnScreenTime - duration)) / OnScreenTime;
                }

                if (currentPosition >= timing && currentPosition < timing + duration)
                    result.LinePositionInLane = 1;

                if (currentPosition >= timing + duration)
                {
                    result.PositionInLane = (laneLength + (currentPosition - timing - duration) / 1000f * Speed) /
                                            laneLength;
                    result.HoldSpriteLength = _initialHoldLength;

                    result.LinePositionInLane =
                        1 + (currentPosition - timing - duration) / 1000f * Speed / laneLength;
                }
            }
        }

        public override void RegisterTapEvent()
        {
            SimulatedSensor.OnTap += JudgeHead;
            SimulatedSensor.OnLeave += OnLeave;
        }

        private void JudgeHead(object sender, TouchEventArgs e)
        {
            var parsed = int.TryParse(e.SensorId.Replace("A", ""), out var touchedLane);
            if (!parsed)
                return;

            if (touchedLane != lane)
                return;

            var noteGenerator = NoteGenerator.Instance;

            if (indexInLane != 0 && !noteGenerator.LaneList[lane - 1][indexInLane - 1].headJudged)
                return;

            var deltaTiming = timing - ChartPlayer.Instance.GetTime(true) + ChartPlayer.Instance.judgeDelay;

            var judgeSettings = ChartPlayer.Instance.tapJudgeSettings;

            var state = GetJudgeState(deltaTiming, judgeSettings);

            headJudged = state.judged;

            if (!headJudged)
                return;

            _headJudgeState = state.Item1;

            isHeadFast = state.isFast;

            ChartPlayer.Instance.holdRippleAnimators[lane - 1].SetTrigger(
                _headJudgeState switch
                {
                    JudgeState.CriticalPerfect => "HoldPerfect",
                    JudgeState.SemiCriticalPerfect => "HoldPerfect",
                    JudgeState.Perfect => "HoldPerfect",
                    JudgeState.Good => "HoldGood",
                    JudgeState.QuarterGreat => "HoldGreat",
                    JudgeState.SemiGreat => "HoldGreat",
                    JudgeState.Great => "HoldGreat",
                    _ => "HoldPerfect"
                });

            if (_headJudgeState is not JudgeState.CriticalPerfect and not JudgeState.Miss)
            {
                var settings = SettingsPool.GetValue("fast_late_display_level");

                switch (settings)
                {
                    case 0:
                        break;
                    case 1:
                        if (_headJudgeState is not JudgeState.SemiCriticalPerfect and not JudgeState.Perfect)
                        {
                            OffsetDisplayAnimator.SetTrigger(isHeadFast ? "ShowFast" : "ShowLate");
                            if (isHeadFast)
                                Scoreboard.FastCount++;
                            else
                                Scoreboard.LateCount++;
                        }

                        break;
                    case 2:
                        OffsetDisplayAnimator.SetTrigger(isHeadFast ? "ShowFast" : "ShowLate");
                        if (isHeadFast)
                            Scoreboard.FastCount++;
                        else
                            Scoreboard.LateCount++;
                        break;
                }
            }

            AreaARipple.AreaARipples.Find(x => x.sensorId == "A" + lane).CancelAnimation();

            _glowAnimator.SetTrigger("Glow");

            SimulatedSensor.OnTap -= JudgeHead;
        }

        private void OnLeave(object sender, TouchEventArgs e)
        {
            var parsed = int.TryParse(e.SensorId.Replace("A", ""), out var touchedLane);
            if (!parsed)
                return;

            if (touchedLane != lane)
                return;

            if (!headJudged)
                return;

            var time = ChartPlayer.Instance.GetTime(true);

            if (time < timing || time > timing + duration +
                ChartPlayer.Instance.holdTailJudgeSettings.greatTiming + ChartPlayer.Instance.judgeDelay)
                return;

            ChartPlayer.Instance.holdRippleAnimators[lane - 1].SetTrigger("Reset");

            _glowAnimator.SetTrigger("Reset");

            var deltaTiming = timing + duration - time + ChartPlayer.Instance.judgeDelay;

            var absDeltaTiming = math.abs(deltaTiming);

            var judgeSettings = ChartPlayer.Instance.holdTailJudgeSettings;

            isFast = deltaTiming > 0;

            if (absDeltaTiming > judgeSettings.greatTiming)
                _holdTailJudgeState = JudgeState.Good;
            if (absDeltaTiming <= judgeSettings.greatTiming && absDeltaTiming > judgeSettings.perfectTiming)
                _holdTailJudgeState = JudgeState.Great;
            if (absDeltaTiming <= judgeSettings.perfectTiming)
                _holdTailJudgeState = JudgeState.CriticalPerfect;

            if (_holdTailJudgeState == JudgeState.CriticalPerfect)
                judgeState = _headJudgeState;
            else if (_holdTailJudgeState == JudgeState.Great)
                judgeState = _headJudgeState == JudgeState.Good ? JudgeState.Good : JudgeState.Great;
            else if (_holdTailJudgeState == JudgeState.Good) judgeState = JudgeState.Good;

            Scoreboard.HoldCount.Count(judgeState);

            PlayJudgeSound(false, judgeState);

            Scoreboard.Combo++;

            PlayJudgeAnimation();

            holdSpriteRenderer.enabled = false;
            holdEndSpriteRenderer.enabled = false;

            SimulatedSensor.OnLeave -= OnLeave;

            holdJudged = true;

            NoteContentRoot.SetActive(false);
        }

        protected override void LateStart()
        {
            transform.position = Vector3.zero;
            holdTransform.localScale = Vector3.zero;
            holdTransform.position *= NoteGenerator.Instance.originCircleScale;
            holdSpriteRenderer.color = new Color(1, 1, 1, 0);
            holdEndSpriteRenderer.enabled = false;
            _initialHoldLength = holdSpriteRenderer.size.y;

            var laneIndex = lane - 1;
            var endPoint = Lanes.Instance.endPoints[laneIndex];
            var startPoint = Lanes.Instance.startPoints[laneIndex];
            _distance = (endPoint.position - startPoint.position).magnitude;

            _glowAnimator = GetComponent<Animator>();

            Scoreboard.HoldCount.TotalCount++;
        }

        private class HoldTransform
        {
            public float Alpha;
            public float HoldSpriteLength;

            public float LinePositionInLane;
            public float PositionInLane;
            public Vector3 Scale;

            public bool Shown;
            public float TailDotPositionInLane;

            public bool TailDotShown;
        }
    }
}