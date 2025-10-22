using System;
using LitMotion;
using Unity.Mathematics;
using UnityEngine;

namespace Notes.Taps
{
    public class Hold : TapBasedNote
    {
        public SpriteRenderer holdSpriteRenderer;
        public Transform holdTransform;

        public bool holdJudged;
        
        public int duration;
        private bool _compensationApplied;
        private int _disappearTime;
        private float _distance;

        private bool _emerging;
        private float _grossHoldSize;
        private bool _holdDone;
        private float _initialHoldSize;
        private bool _lineMoving;
        private bool _moving;

        private int _nowEmergingDuration;
        
        private JudgeState _holdTailJudgeState;
        private JudgeState _headJudgeState;
        
        private Animator _glowAnimator;

        public void Update()
        {
            if (!ChartPlayer.Instance.isPlaying || holdJudged)
                return;

            if (ChartPlayer.Instance.time > timing + ChartPlayer.Instance.tapJudgeSettings.lateGoodTiming &&
                !headJudged)
            {
                headJudged = true;
                holdJudged = true;
                judgeState = JudgeState.Miss;

                PlayJudgeAnimation();

                SimulatedSensor.OnTap -= JudgeHead;
                SimulatedSensor.OnLeave -= OnLeave;
            }

            if (ChartPlayer.Instance.time >
                timing + duration &&
                headJudged && !holdJudged)
            {
                ChartPlayer.Instance.holdRippleAnimators[lane - 1].SetTrigger("Reset");
            }
            
            if (ChartPlayer.Instance.time > timing + duration + ChartPlayer.Instance.holdTailJudgeSettings.greatTiming &&
                headJudged && !holdJudged)
            {
                holdJudged = true;
                _holdTailJudgeState = JudgeState.Good;
                judgeState = JudgeState.Good;

                PlayJudgeAnimation();
                
                _glowAnimator.SetTrigger("Reset");

                SimulatedSensor.OnLeave -= OnLeave;
            }

            if (ChartPlayer.Instance.time > timing - 2 * EmergingDuration &&
                ChartPlayer.Instance.time < timing - 1 * EmergingDuration && !_emerging)
            {
                _emerging = true;

                transform.position = Vector3.zero;

                LMotion.Create(0, 1f, EmergingDuration / 1000f).WithEase(Ease.OutSine)
                    .Bind(x =>
                    {
                        holdSpriteRenderer.color = new Color(1, 1, 1, x);
                        lineSpriteRenderer.color = new Color(1, 1, 1, x);
                    });
                LMotion.Create(0, 1f, EmergingDuration / 1000f).WithEase(Ease.Linear)
                    .Bind(x => holdTransform.localScale = x * Vector3.one);
            }

            if (ChartPlayer.Instance.time > timing - 1 * EmergingDuration && _emerging && !_moving)
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
                    transform.position = NoteGenerator.Instance.outOfScreenPosition;
                    _moving = false;
                    _holdDone = false;
                }

                holdTransform.Translate(Speed * Time.deltaTime * Vector3.up);
                return;
            }

            if (!_moving)
                return;

            holdTransform.localScale = Vector3.one;

            _nowEmergingDuration = EmergingDuration - (timing - ChartPlayer.Instance.time);

            if (_nowEmergingDuration <= Math.Max(EmergingDuration, duration))
            {
                if ((_distance > _grossHoldSize && EmergingDuration < duration) ||
                    (EmergingDuration >= duration && _nowEmergingDuration < duration))
                {
                    _grossHoldSize += Speed * Time.deltaTime;
                    holdTransform.Translate(0.5f * Speed * Time.deltaTime * Vector3.up);
                }
                else if (_nowEmergingDuration > duration)
                    holdTransform.Translate(Speed * Time.deltaTime * Vector3.up);
            }
            else
            {
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

            if (ChartPlayer.Instance.time > timing)
                TrimHold(_nowEmergingDuration < duration);

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

            var deltaTiming = timing - ChartPlayer.Instance.time;
            
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

            if (ChartPlayer.Instance.time < timing || ChartPlayer.Instance.time > timing + duration + 
                ChartPlayer.Instance.holdTailJudgeSettings.greatTiming)
                return;
            
            ChartPlayer.Instance.holdRippleAnimators[lane - 1].SetTrigger("Reset");

            _glowAnimator.SetTrigger("Reset");
            
            var deltaTime = timing + duration - ChartPlayer.Instance.time;
            
            var absDeltaTiming = math.abs(deltaTime);

            var judgeSettings = ChartPlayer.Instance.holdTailJudgeSettings;

            isFast = deltaTime > 0;

            if (absDeltaTiming > judgeSettings.greatTiming)
                _holdTailJudgeState = JudgeState.Good;
            if (absDeltaTiming <= judgeSettings.greatTiming && absDeltaTiming > judgeSettings.perfectTiming)
                _holdTailJudgeState = JudgeState.Great;
            if (absDeltaTiming <= judgeSettings.perfectTiming)
                _holdTailJudgeState = JudgeState.Perfect;

            if (_holdTailJudgeState == JudgeState.Perfect)
                judgeState = _headJudgeState;
            else if (_holdTailJudgeState == JudgeState.Great)
            {
                judgeState = _headJudgeState == JudgeState.Good ? JudgeState.Good : JudgeState.Great;
            }
            else if (_holdTailJudgeState == JudgeState.Good)
            {
                judgeState = JudgeState.Good;
            }
            
            PlayJudgeAnimation();
            
            SimulatedSensor.OnLeave -= OnLeave;

            holdJudged = true;
        }

        protected override void LateStart()
        {
            transform.position = Vector3.zero;
            holdTransform.localScale = Vector3.zero;
            holdTransform.position *= NoteGenerator.Instance.originCircleScale;
            holdSpriteRenderer.color = new Color(1, 1, 1, 0);
            transform.position = NoteGenerator.Instance.outOfScreenPosition;
            _initialHoldSize = holdSpriteRenderer.size.y;

            var laneIndex = lane - 1;
            var endPoint = Lanes.Instance.endPoints[laneIndex];
            var startPoint = Lanes.Instance.startPoints[laneIndex];
            _distance = (endPoint.position - startPoint.position).magnitude;

            _glowAnimator = GetComponent<Animator>();
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