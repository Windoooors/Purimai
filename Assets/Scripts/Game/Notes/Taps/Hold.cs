using System;
using LitMotion;
using LitMotion.Extensions;
using UI.Result;
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

        private bool _compensationApplied;
        private int _disappearTime;
        private float _distance;

        private bool _emerging;

        private Animator _glowAnimator;
        private float _grossHoldSize;
        private JudgeState _headJudgeState;
        private bool _holdDone;

        private JudgeState _holdTailJudgeState;
        private float _initialHoldSize;
        private bool _lineMoving;
        private bool _moving;

        private int _nowEmergingTimePosition;

        private void Update()
        {
            if (!ChartPlayer.Instance.isPlaying || holdJudged)
                return;

            if (ChartPlayer.Instance.time > timing + ChartPlayer.Instance.tapJudgeSettings.lateGoodTiming +
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

            if (ChartPlayer.Instance.time >
                timing + duration &&
                headJudged && !holdJudged)
                ChartPlayer.Instance.holdRippleAnimators[lane - 1].SetTrigger("Reset");

            if (ChartPlayer.Instance.time >
                timing + duration + ChartPlayer.Instance.holdTailJudgeSettings.greatTiming +
                ChartPlayer.Instance.judgeDelay &&
                headJudged && !holdJudged)
            {
                holdJudged = true;
                _holdTailJudgeState = JudgeState.Good;
                judgeState = JudgeState.Good;

                PlayJudgeAnimation();

                holdSpriteRenderer.enabled = false;
                holdEndSpriteRenderer.enabled = false;

                _glowAnimator.SetTrigger("Reset");

                SimulatedSensor.OnLeave -= OnLeave;
            }

            if (ChartPlayer.Instance.time >= timing - 2 * EmergingDuration &&
                ChartPlayer.Instance.time < timing - 1 * EmergingDuration && !_emerging)
            {
                _emerging = true;

                NoteContentRoot.SetActive(true);

                lineSpriteRenderer.enabled = true;
                holdSpriteRenderer.enabled = true;

                transform.position = Vector3.zero;

                var animationDuration = EmergingDuration / 1000f / (IsAdxFlowSpeedStyle ? 2 : 1);
                var animationDelay = IsAdxFlowSpeedStyle ? EmergingDuration / 1000f / 2 : 0;

                LMotion.Create(0, 1f, animationDuration)
                    .WithDelay(animationDelay)
                    .WithEase(Ease.OutSine)
                    .Bind(x =>
                    {
                        holdSpriteRenderer.color = new Color(1, 1, 1, x);
                        lineSpriteRenderer.color = new Color(1, 1, 1, x);
                    });
                LMotion.Create(Vector3.zero, Vector3.one, animationDuration)
                    .WithDelay(animationDelay)
                    .WithEase(Ease.Linear)
                    .BindToLocalScale(holdTransform);
            }

            if (ChartPlayer.Instance.time >= timing - 1 * EmergingDuration && _emerging && !_moving)
            {
                _emerging = false;
                _moving = true;
                _lineMoving = true;
            }

            if (ChartPlayer.Instance.time >= timing && _lineMoving)
            {
                lineTransform.localScale = Vector3.one;
                _lineMoving = false;
            }

            if (_lineMoving) lineTransform.localScale += LineExpansionSpeed * Time.deltaTime * Vector3.one;

            if (_holdDone)
            {
                if (ChartPlayer.Instance.time > _disappearTime)
                {
                    lineSpriteRenderer.enabled = false;
                    holdSpriteRenderer.enabled = false;
                    holdEndSpriteRenderer.enabled = false;
                    _moving = false;
                    _holdDone = false;
                }

                holdTransform.Translate(Speed * Time.deltaTime * Vector3.up);
                return;
            }

            if (!_moving)
                return;

            holdTransform.localScale = Vector3.one;

            _nowEmergingTimePosition = EmergingDuration - (timing - ChartPlayer.Instance.time);

            if (_nowEmergingTimePosition <= Math.Max(EmergingDuration, duration))
            {
                if ((_distance > _grossHoldSize && EmergingDuration < duration) ||
                    (EmergingDuration >= duration && _nowEmergingTimePosition < duration))
                {
                    _grossHoldSize += Speed * Time.deltaTime;
                    holdTransform.Translate(0.5f * Speed * Time.deltaTime * Vector3.up);
                }
                else if (_nowEmergingTimePosition > duration)
                {
                    holdEndSpriteRenderer.enabled = true;
                    holdEndSpriteRenderer.color = Color.white;
                    holdEnd.Translate(Speed * Time.deltaTime * Vector3.up);
                    holdTransform.Translate(Speed * Time.deltaTime * Vector3.up);
                }
            }
            else
            {
                holdEndSpriteRenderer.enabled = true;
                holdEndSpriteRenderer.color = Color.white;
                holdEnd.Translate(Speed * Time.deltaTime * Vector3.up);
                _grossHoldSize -= Speed * Time.deltaTime;
                holdTransform.Translate(0.5f * Speed * Time.deltaTime * Vector3.up);
            }

            if (_grossHoldSize < 0)
            {
                holdTransform.Translate(0.5f * _grossHoldSize * Vector3.up);
                _grossHoldSize = 0;
                _disappearTime = ChartPlayer.Instance.time + 100;
                _holdDone = true;
                holdSpriteRenderer.size = new Vector2(holdSpriteRenderer.size.x, _initialHoldSize);
                return;
            }

            if (ChartPlayer.Instance.time >= timing)
                TrimHold(_nowEmergingTimePosition < duration);

            holdSpriteRenderer.size = new Vector2(holdSpriteRenderer.size.x, _initialHoldSize + _grossHoldSize);
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

            var deltaTiming = timing - ChartPlayer.Instance.GetTime() + ChartPlayer.Instance.judgeDelay;

            var judgeSettings = ChartPlayer.Instance.tapJudgeSettings;

            var state = GetJudgeState(deltaTiming, judgeSettings);

            headJudged = state.judged;

            if (!headJudged)
                return;

            _headJudgeState = state.Item1;

            isFast = state.isFast;

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

            var time = ChartPlayer.Instance.GetTime();

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
                _holdTailJudgeState = JudgeState.Perfect;

            if (_holdTailJudgeState == JudgeState.Perfect)
                judgeState = _headJudgeState;
            else if (_holdTailJudgeState == JudgeState.Great)
                judgeState = _headJudgeState == JudgeState.Good ? JudgeState.Good : JudgeState.Great;
            else if (_holdTailJudgeState == JudgeState.Good) judgeState = JudgeState.Good;

            Scoreboard.HoldCount.Count(judgeState);

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
            holdSpriteRenderer.enabled = false;
            lineSpriteRenderer.enabled = false;
            _initialHoldSize = holdSpriteRenderer.size.y;

            var laneIndex = lane - 1;
            var endPoint = Lanes.Instance.endPoints[laneIndex];
            var startPoint = Lanes.Instance.startPoints[laneIndex];
            _distance = (endPoint.position - startPoint.position).magnitude;

            _glowAnimator = GetComponent<Animator>();

            Scoreboard.HoldCount.TotalCount++;
        }

        private void TrimHold(bool forceLong = false)
        {
            var roughDistance =
                (holdTransform.position - Lanes.Instance.startPoints[lane - 1].position).magnitude +
                _grossHoldSize / 2;

            var deltaDistance = _distance - roughDistance;

            if (!forceLong)
                _grossHoldSize += deltaDistance;
            else
                _grossHoldSize = _distance;
            holdTransform.Translate(0.5f * deltaDistance * Vector3.up);
        }
    }
}