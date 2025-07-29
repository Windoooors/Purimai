using System;
using LitMotion;
using UnityEngine;

namespace Notes.Taps
{
    public class Hold : TapBasedNote
    {
        public SpriteRenderer holdSpriteRenderer;
        public Transform holdTransform;

        public int duration;
        private bool _compensationApplied;
        private float _distance;

        private bool _emerging;
        private float _grossHoldSize;
        private float _initialHoldSize;
        private bool _lineMoving;
        private bool _moving;

        private int _nowEmergingDuration;

        public void Update()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            if (ChartPlayer.Instance.time > timing - 2 * EmergingDuration &&
                ChartPlayer.Instance.time < timing - 1 * EmergingDuration && !_emerging)
            {
                _emerging = true;
                holdSpriteRenderer.enabled = true;
                lineSpriteRenderer.enabled = true;

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

            if (ChartPlayer.Instance.time > timing + 100)
            {
                lineSpriteRenderer.enabled = false;
                _lineMoving = false;
            }

            if (_lineMoving) lineTransform.localScale += LineExpansionSpeed * Time.deltaTime * Vector3.one;

            if (_moving)
            {
                holdTransform.localScale = Vector3.one;

                _nowEmergingDuration = EmergingDuration - (timing - ChartPlayer.Instance.time);

                if (_nowEmergingDuration <= Math.Max(EmergingDuration, duration))
                {
                    if (_distance > _grossHoldSize && EmergingDuration < duration)
                    {
                        _grossHoldSize += Speed * Time.deltaTime;
                        holdTransform.Translate(0.5f * Speed * Time.deltaTime * Vector3.up);
                    }
                    else if (EmergingDuration >= duration)
                    {
                        if (_nowEmergingDuration < duration)
                        {
                            _grossHoldSize += Speed * Time.deltaTime;
                            holdTransform.Translate(0.5f * Speed * Time.deltaTime * Vector3.up);
                        }
                        else
                        {
                            holdTransform.Translate(Speed * Time.deltaTime * Vector3.up);
                        }
                    }
                    else
                    {
                        TrimHold(true);
                    }
                }
                else
                {
                    _grossHoldSize -= Speed * Time.deltaTime;
                    holdTransform.Translate(0.5f * Speed * Time.deltaTime * Vector3.up);
                }

                if (ChartPlayer.Instance.time > timing)
                    TrimHold();

                if (_grossHoldSize < 0)
                {
                    holdSpriteRenderer.enabled = false;
                    _moving = false;
                }

                holdSpriteRenderer.size = new Vector2(holdSpriteRenderer.size.x, _initialHoldSize + _grossHoldSize);
            }
        }

        protected override void LateStart()
        {
            holdTransform.localScale = Vector3.zero;
            holdTransform.position *= NoteGenerator.Instance.originCircleScale;
            holdSpriteRenderer.color = new Color(1, 1, 1, 0);
            holdSpriteRenderer.enabled = false;
            _initialHoldSize = holdSpriteRenderer.size.y;

            var laneIndex = lane - 1;
            var endPoint = Lanes.Instance.endPoints[laneIndex];
            var startPoint = Lanes.Instance.startPoints[laneIndex];
            _distance = (endPoint.position - startPoint.position).magnitude;
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