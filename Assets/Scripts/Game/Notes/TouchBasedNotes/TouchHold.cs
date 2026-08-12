using System.Collections.Generic;
using System.Linq;
using Game.Theming;
using UI.Result;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Notes.TouchBasedNotes
{
    public class TouchHold : TouchBasedNote
    {
        private const float Step = 0.463f;

        private const int HeadIgnoredDuration = 250;
        private const int TailIgnoredDuration = 200;

        private const int ReleaseWaitDuration = 50;
        private const int ReleaseCompensation = 50;

        public int holdDuration;
        public Transform[] touchTransforms;
        public SpriteRenderer[] touchSpriteRenderers;
        public float scale = 1;
        public SpriteRenderer dotSpriteRenderer;
        public SpriteRenderer borderSpriteRenderer;

        public List<TouchHold> touchGroup;

        private bool _alreadyShown;
        private bool _firstCount;

        private bool _headJudgedByTouchGroup;

        private JudgeState _headJudgeState;
        private bool _holdAnimationPlayed;

        private Animator _holdAnimator;
        private bool _holding;

        private bool _holdJudged;

        private Coroutine _holdLeaveCoroutine;

        private bool _holdReleaseJudgable;

        private bool _isHoldSoundPlaying;

        private JudgeManager.JudgeAction _judgeHeadAction;
        private JudgeManager.JudgeAction _judgeHoldAction;
        private JudgeManager.JudgeAction _judgeLeaveAction;

        private MaterialPropertyBlock _materialPropertyBlock;

        private float _releasedTimePeriod;

        private float _releasingTimeBeforeCountStarts;
        private TouchHoldTransform _touchTransform;

        private bool _virtuallyHolding;

        public void SetOrder(int order)
        {
            foreach (var touchSpriteRenderer in touchSpriteRenderers) touchSpriteRenderer.sortingOrder += order;

            borderSpriteRenderer.sortingOrder += order;
            dotSpriteRenderer.sortingOrder += order;
        }

        protected override void LateStart()
        {
            _materialPropertyBlock = new MaterialPropertyBlock();
            _touchTransform = new TouchHoldTransform();

            _materialPropertyBlock.SetFloat("_Phase", 0);

            foreach (var touchTransform in touchTransforms) touchTransform.Translate(Vector3.down * Step * scale);

            foreach (var touchSpriteRenderer in touchSpriteRenderers) touchSpriteRenderer.color = new Color(1, 1, 1, 0);

            borderSpriteRenderer.SetPropertyBlock(_materialPropertyBlock);

            Scoreboard.HoldCount.TotalCount++;

            var animatorIndexBase = sensorId.ToCharArray()[0] switch
            {
                'A' => 8,
                'B' => 16,
                'D' => 24,
                'E' => 32,
                'C' => 40,
                _ => 0
            };

            int.TryParse(sensorId.ToCharArray()[^1].ToString(), out var sensorLane);

            if (sensorLane != 0)
                sensorLane--;

            var animatorIndex = animatorIndexBase + sensorLane;

            _holdAnimator = ChartPlayer.Instance.holdRippleAnimators[animatorIndex];
        }

        public override void RegisterTapEvent()
        {
            var judgeSettings = ChartPlayer.Instance.touchJudgeSettings;

            JudgeManager.Instance.RegisterTap(timing - 100 - judgeSettings.fastGoodTiming,
                timing + 100 + holdDuration + judgeSettings.lateGoodTiming, JudgeHead, out _judgeHeadAction
            );

            JudgeManager.Instance.RegisterHold(timing - 100 - judgeSettings.fastGoodTiming,
                timing + holdDuration + 100, OnHold, out _judgeHoldAction
            );

            JudgeManager.Instance.RegisterLeave(timing - 100 - judgeSettings.fastGoodTiming,
                timing + holdDuration + 100, OnLeave, out _judgeLeaveAction
            );
        }

        private void OnLeave(object sender, TouchEventArgs e)
        {
            if (e.SensorId != sensorId)
                return;

            _holding = false;
        }

        private void OnHold(object sender, TouchEventArgs e)
        {
            if (e.SensorId != sensorId)
                return;

            if (!_holdJudged)
                _holding = true;
        }

        private void JudgeHead(object sender, TouchEventArgs e)
        {
            if (headJudged)
                return;

            if (e.SensorId != sensorId)
                return;

            var noteGenerator = NoteGenerator.Instance;

            if (indexInLane != 0 && !noteGenerator.TouchLanes[sensorId][indexInLane - 1].headJudged)
                return;

            var deltaTiming = timing - ChartPlayer.Instance.TimeInMilliseconds + ChartPlayer.Instance.judgeDelay;

            var judgeSettings = ChartPlayer.Instance.touchJudgeSettings;

            var state = GetJudgeState(deltaTiming, false, judgeSettings);

            headJudged = state.judged;

            if (!headJudged)
                return;

            _headJudgeState = state.Item1;

            isFast = state.isFast;

            if (GetTouchGroupHeadJudgedProportion() > 0.5f && !_headJudgedByTouchGroup)
            {
                _headJudgedByTouchGroup = true;

                touchGroup.ForEach(x =>
                {
                    if (x == this)
                        return;

                    x._headJudgedByTouchGroup = true;
                    x.JudgeHead(sender, new TouchEventArgs(x.sensorId));
                });
            }

            if (!_isHoldSoundPlaying)
            {
                _isHoldSoundPlaying = true;
                TouchHoldSoundHelper.Instance.Play(this);
            }

            _materialPropertyBlock.SetInteger("_Gray", 0);

            _judgeHeadAction.Enabled = false;

            PlayHoldAnimation(_headJudgeState);
            _holdAnimationPlayed = true;
        }

        private void PlayHoldAnimation(JudgeState targetJudgeState)
        {
            _holdAnimator.SetTrigger(ThemeManager.HoldColorRelatedHoldEffect
                ? isEach switch
                {
                    true => "HoldPerfect",
                    false => "HoldGreat"
                }
                : targetJudgeState switch
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
        }

        private float GetTouchGroupHeadJudgedProportion()
        {
            return touchGroup.Count(x => x.headJudged) / (float)touchGroup.Count;
        }

        private float GetTouchGroupHoldingProportion()
        {
            return touchGroup.Count(x => x._holding && !x._holdJudged) / (float)touchGroup.Count;
        }

        public override void AddAutoPlayKeyFrame()
        {
            var list = AutoPlayer.KeyFrameManager.GetKeyFrames(sensorId);

            list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, timing));
            list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.HoldStart, timing));
            list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.HoldEnd, timing + holdDuration));
        }

        public override void ManualUpdate()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            GetTouchHoldTransform(ref _touchTransform);

            if (_touchTransform.Shown && !_alreadyShown) _alreadyShown = true;

            if ((!_touchTransform.Shown && !headJudged && !_alreadyShown) ||
                (_alreadyShown && headJudged && _holdJudged))
            {
                NoteContentRoot.SetActive(false);
                return;
            }

            _holdReleaseJudgable = ChartPlayer.Instance.TimeInMilliseconds >
                                   timing + ChartPlayer.Instance.judgeDelay + HeadIgnoredDuration &&
                                   ChartPlayer.Instance.TimeInMilliseconds < timing + holdDuration -
                                   TailIgnoredDuration + ChartPlayer.Instance.judgeDelay;

            _virtuallyHolding = GetTouchGroupHoldingProportion() > 0.5f || _holding;

            if (!_virtuallyHolding && headJudged && _holdReleaseJudgable)
            {
                if (_releasingTimeBeforeCountStarts >= ReleaseWaitDuration / 1000f)
                {
                    _releasedTimePeriod += Time.deltaTime * 1000;

                    if (!_firstCount)
                    {
                        _firstCount = true;
                        _releasedTimePeriod += _releasingTimeBeforeCountStarts * 1000;
                    }
                }
                else
                {
                    _releasingTimeBeforeCountStarts += Time.deltaTime;
                }
            }

            if (_virtuallyHolding && _holdReleaseJudgable && !_holdJudged)
            {
                if (!_holdAnimationPlayed)
                {
                    _holdAnimationPlayed = true;

                    if (_headJudgeState == JudgeState.Miss)
                        PlayHoldAnimation(JudgeState.Good);
                }

                _releasingTimeBeforeCountStarts = 0;
                _firstCount = false;
            }

            if (_touchTransform.Shown && !headJudged && !NoteContentRoot.activeSelf)
                NoteContentRoot.SetActive(true);

            if (!headJudged && ChartPlayer.Instance.TimeInMilliseconds >
                timing + ChartPlayer.Instance.touchJudgeSettings.lateGoodTiming + ChartPlayer.Instance.judgeDelay)
            {
                headJudged = true;
                _headJudgeState = JudgeState.Miss;
            }

            if (!_holdJudged && ChartPlayer.Instance.TimeInMilliseconds >
                timing + holdDuration + ChartPlayer.Instance.judgeDelay && headJudged
               )
            {
                _holdJudged = true;
                _judgeHoldAction.Enabled = false;
                _judgeLeaveAction.Enabled = false;

                _holdAnimator.SetTrigger("Reset");

                TouchHoldSoundHelper.Instance.Stop(this);

                if (_headJudgeState == JudgeState.Miss) _releasedTimePeriod += ReleaseCompensation;

                var heldDuration = holdDuration - _releasedTimePeriod - HeadIgnoredDuration - TailIgnoredDuration;
                var checkedDuration = holdDuration - HeadIgnoredDuration - TailIgnoredDuration;

                if (checkedDuration <= 0)
                {
                    judgeState = _headJudgeState;
                }
                else
                {
                    var holdRate = heldDuration / checkedDuration;
                    if (holdRate.CompareTo(1f) == 0)
                    {
                        if (_headJudgeState is JudgeState.CriticalPerfect)
                            judgeState = JudgeState.CriticalPerfect;
                        else if (_headJudgeState is JudgeState.Perfect or JudgeState.SemiCriticalPerfect)
                            judgeState = JudgeState.SemiCriticalPerfect;
                        else if (_headJudgeState is not JudgeState.Miss)
                            judgeState = JudgeState.Great;
                        else if (_headJudgeState is JudgeState.Miss) judgeState = JudgeState.Good;
                    }
                    else if (holdRate is >= 0.67f and < 1f)
                    {
                        if (_headJudgeState is JudgeState.CriticalPerfect or JudgeState.Perfect
                            or JudgeState.SemiCriticalPerfect)
                            judgeState = JudgeState.SemiCriticalPerfect;
                        else if (_headJudgeState is not JudgeState.Miss)
                            judgeState = JudgeState.Great;
                        else if (_headJudgeState is JudgeState.Miss) judgeState = JudgeState.Good;
                    }
                    else if (holdRate is >= 0.33f and < 0.67f)
                    {
                        if (_headJudgeState is not (JudgeState.Miss or JudgeState.Good))
                            judgeState = JudgeState.Great;
                        else
                            judgeState = JudgeState.Good;
                    }
                    else if (holdRate is >= 0.05f and < 0.33f)
                    {
                        judgeState = JudgeState.Good;
                    }
                    else if (holdRate < 0.05f)
                    {
                        if (_headJudgeState is not JudgeState.Miss)
                            judgeState = JudgeState.Good;
                        else
                            judgeState = JudgeState.Miss;
                    }
                }

                _holding = false;

                NoteContentRoot.SetActive(false);

                PlayJudgeAnimation();
                PlayJudgeSound(judgeState);
                Scoreboard.HoldCount.Count(judgeState);

                if (judgeState == JudgeState.Miss)
                    Scoreboard.ResetCombo();
                else
                    Scoreboard.Combo++;
            }

            foreach (var touchTransform in touchTransforms)
            {
                var angleRad = touchTransform.eulerAngles.z * Mathf.Deg2Rad;
                var direction = new Vector3(-Mathf.Sin(angleRad), Mathf.Cos(angleRad), 0);

                touchTransform.position = Vector3.Lerp(transform.position - direction * Step, transform.position,
                    Mathf.Pow(_touchTransform.Position, 2));
            }

            if (_touchTransform.ShowBorder && !_holdJudged)
            {
                var phase = (ChartPlayer.Instance.TimeInMilliseconds - timing) / holdDuration;
                math.clamp(phase, 0, 1);

                _materialPropertyBlock.SetFloat("_Phase", phase);

                if (_holdReleaseJudgable)
                    if (!_virtuallyHolding)
                    {
                        if (_isHoldSoundPlaying)
                        {
                            _isHoldSoundPlaying = false;
                            TouchHoldSoundHelper.Instance.Stop(this);
                        }
                    }
                    else
                    {
                        if (!_isHoldSoundPlaying)
                        {
                            _isHoldSoundPlaying = true;
                            TouchHoldSoundHelper.Instance.Play(this);
                        }
                    }

                if (_holdReleaseJudgable)
                    if (_headJudgeState == JudgeState.Miss || !_virtuallyHolding || !headJudged)
                        _materialPropertyBlock.SetInteger("_Gray", 1);
                    else
                        _materialPropertyBlock.SetInteger("_Gray", 0);

                borderSpriteRenderer.SetPropertyBlock(_materialPropertyBlock);
            }

            var color = new Color(1, 1, 1, _touchTransform.Alpha);
            foreach (var touchSpriteRenderer in touchSpriteRenderers) touchSpriteRenderer.color = color;
        }

        private void GetTouchHoldTransform(ref TouchHoldTransform result)
        {
            if (result == null)
                return;

            var currentPosition = ChartPlayer.Instance.TimeInMilliseconds;

            var startEmergingTiming = timing - TouchOnScreenTime - TouchOnScreenTime / 4f;

            var startMovingTiming = timing - TouchOnScreenTime;

            if (currentPosition < startEmergingTiming - 100 ||
                currentPosition > timing + holdDuration ||
                (indexInLane - 1 >= 0 && !NoteGenerator.Instance.TouchLanes[sensorId][indexInLane - 1].headJudged))
            {
                result.Shown = false;
                return;
            }

            if (currentPosition > startEmergingTiming && currentPosition < startMovingTiming)
            {
                var factor = (currentPosition - emergingTime) / (TouchOnScreenTime / 4f);

                result.Alpha = factor;
                result.Position = 0;
                result.Shown = true;

                return;
            }

            if (currentPosition >= startMovingTiming)
            {
                var factor = (currentPosition - startMovingTiming) / TouchOnScreenTime;

                result.Alpha = 1;
                result.Position = factor;
                result.Shown = true;

                if (currentPosition > timing)
                    result.ShowBorder = true;

                return;
            }

            result.Alpha = 0;
            result.Position = 0;
            result.Shown = false;
        }

        private class TouchHoldTransform
        {
            public float Alpha;
            public float Position;
            public bool ShowBorder;
            public bool Shown;
        }
    }
}