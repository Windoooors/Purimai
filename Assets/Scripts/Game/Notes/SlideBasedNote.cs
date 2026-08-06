using System;
using System.Collections;
using System.Collections.Generic;
using Game.ChartManagement;
using UI.Result;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Notes
{
    public abstract class SlideBasedNote : NoteBase
    {
        public Segment[] Segments;
        private bool _concealed;
        private bool _haveShown;
        private JudgeManager.JudgeAction _holdJudgeAction;
        private bool _isFast;

        private Animator _judgeDisplayAnimator;
        private JudgeState _judgeState;

        private bool _launchSoundPlayed;
        private JudgeManager.JudgeAction _leaveJudgeAction;

        private MaterialPropertyBlock _materialPropertyBlock;

        private int _showJudgeDisplayTiming = -1;
        private bool _slidedHalf;
        private SlideTransform _slideTransform;

        private Texture _starTexture;

        protected GameObject SlideContentRoot;
        protected bool Slided;
        public IStarMovementController[] Stars { get; private set; }
        public NoteDataObject.SlideDataObject SlideDataObject { get; private set; }
        public SpriteRenderer JudgeDisplaySpriteRenderer { get; private set; }

        protected int Order { get; private set; }
        public int Timing { get; private set; }
        public bool IsEach { get; private set; }
        public bool IsBreak { get; private set; }
        public bool SuddenlyAppears { get; private set; }
        protected int WaitDuration { get; private set; }
        public int SlideDuration { get; private set; }

        public int JudgeTiming { get; private set; }
        public int StarInLastSegmentDuration { get; private set; }

        private void Start()
        {
            emergingTime = Timing - ChartPlayer.Instance.timeGapBeforeSlideStartsAppearing;
        }

        public void Initialize(NoteDataObject.SlideDataObject slideDataObject, bool isSlideEach, bool isSlideBreak,
            int noteTiming,
            ref int slideArrowOrder)
        {
            _materialPropertyBlock = new MaterialPropertyBlock();
            _slideTransform = new SlideTransform();

            Order = -slideArrowOrder;
            Timing = noteTiming;
            IsEach = isSlideEach;
            IsBreak = isSlideBreak;
            WaitDuration = slideDataObject.WaitDuration;
            SlideDuration = slideDataObject.SlideDuration;

            SuddenlyAppears = slideDataObject.SuddenlyAppears;
            transform.position = Vector3.zero;

            InitializePathRotation();
            InitializeVectorGraphicsUtility();
            InitializeSlideSensorIds();

            slideArrowOrder -= GenerateSlideArrowObjects();

            var dataPair = InitializeSlideSegments();
            JudgeTiming = dataPair.judgeTiming;
            StarInLastSegmentDuration = dataPair.starInLastSegmentDuration;
            Segments = dataPair.segments;

            SlideDataObject = slideDataObject;
            Stars = GetStars();

            JudgeDisplaySpriteRenderer = GetJudgeDisplaySpriteRenderer();
            _judgeDisplayAnimator = JudgeDisplaySpriteRenderer.GetComponent<Animator>();
            _judgeDisplayAnimator.enabled = true;

            JudgeDisplaySpriteRenderer.sortingOrder -= Order;

            foreach (var starMovementController in Stars)
            {
                starMovementController.SetStarOrder(-Order);
                starMovementController.Initialize();
            }

            SlideContentRoot = new GameObject("SlideContent");
            SlideContentRoot.transform.SetParent(transform);

            var children = transform.GetComponentsInChildren<Transform>();

            foreach (var child in children) child.parent = SlideContentRoot.transform;

            SlideContentRoot.SetActive(false);

            var tapJudgeSettings = ChartPlayer.Instance.tapJudgeSettings;
            var slideJudgeSettings = ChartPlayer.Instance.slideJudgeSettings;

            JudgeManager.Instance.RegisterHold(Timing - tapJudgeSettings.fastGoodTiming - 100,
                Timing + SlideDuration + WaitDuration + 100 + slideJudgeSettings.lateGoodTiming, OnHoldSlidePath,
                out _holdJudgeAction);
            JudgeManager.Instance.RegisterLeave(Timing - tapJudgeSettings.fastGoodTiming - 100,
                Timing + SlideDuration + WaitDuration + 100 + slideJudgeSettings.lateGoodTiming, OnLeaveSlidePath,
                out _leaveJudgeAction);

            if (IsBreak)
                Scoreboard.BreakCount.TotalCount++;
            else
                Scoreboard.SlideCount.TotalCount++;

            _starTexture = (IsBreak, IsEach) switch
            {
                (true, _) => NoteGenerator.Instance.breakStarSprite.texture,
                (false, true) => NoteGenerator.Instance.eachStarSprite.texture,
                (false, false) => NoteGenerator.Instance.starSprite.texture
            };
        }

        protected void Judge()
        {
            if (Slided)
                return;

            var deltaTiming = JudgeTiming - ChartPlayer.Instance.TimeInMilliseconds +
                              ChartPlayer.Instance.judgeDelay;

            _isFast = deltaTiming > 0;

            var absDeltaTiming = math.abs(deltaTiming);

            var judgeSettings = ChartPlayer.Instance.slideJudgeSettings;

            var isFast = deltaTiming > 0;

            var compensatedPerfectTiming = judgeSettings.perfectTiming; //+ _starInLastSegmentDuration / 8;

            if (absDeltaTiming <=
                (compensatedPerfectTiming < judgeSettings.fastGoodTiming
                    ? compensatedPerfectTiming
                    : judgeSettings.fastGoodTiming))
                _judgeState = JudgeState.CriticalPerfect;
            else if (absDeltaTiming <= judgeSettings.greatTiming && absDeltaTiming > judgeSettings.perfectTiming)
                _judgeState = JudgeState.Great;
            else if (absDeltaTiming > judgeSettings.greatTiming)
                _judgeState = JudgeState.Good;

            var index = (_judgeState, isFast) switch
            {
                (JudgeState.CriticalPerfect, _) => 0,
                (JudgeState.Great, true) => 1,
                (JudgeState.Good, true) => 2,
                (JudgeState.Great, false) => 3,
                (JudgeState.Good, false) => 4,
                _ => 5
            };

            UpdateJudgeDisplayDirection(index);
            Slided = true;

            _showJudgeDisplayTiming = (int)(ChartPlayer.Instance.TimeInMilliseconds + StarInLastSegmentDuration);

            _holdJudgeAction.Enabled = false;
            _leaveJudgeAction.Enabled = false;
        }

        protected abstract int GenerateSlideArrowObjects();
        protected abstract void InitializeVectorGraphicsUtility();
        protected abstract void InitializePathRotation();
        protected abstract void InitializeSlideSensorIds();

        protected abstract (int judgeTiming, int starInLastSegmentDuration, Segment[] segments)
            InitializeSlideSegments();

        protected abstract IStarMovementController[] GetStars();
        protected abstract void UpdateJudgeDisplayDirection(int displaySpriteIndex);
        protected abstract SpriteRenderer GetJudgeDisplaySpriteRenderer();

        public static bool ArrowOverlapsOnSensor(SpriteRenderer slideArrowSpriteRenderer,
            NoteDataObject.SlideType slideType, Collider2D sensorCollider)
        {
            if (slideType is NoteDataObject.SlideType.RotateLeft
                or NoteDataObject.SlideType.RotateRight
                or NoteDataObject.SlideType.RotateMinorArc)
            {
                var pointResult = sensorCollider.OverlapPoint(slideArrowSpriteRenderer.transform.position);
                return pointResult;
            }

            var colliderAdded = slideArrowSpriteRenderer.TryGetComponent<BoxCollider2D>(out var addedCollider);
            if (!colliderAdded)
                addedCollider = slideArrowSpriteRenderer.gameObject.AddComponent<BoxCollider2D>();

            addedCollider.enabled = true;

            var overlapResults = new List<Collider2D>();

            var filter = new ContactFilter2D();
            filter.SetLayerMask(LayerMask.GetMask("Sensors"));

            addedCollider.Overlap(filter, overlapResults);

            Destroy(addedCollider);

            var result = overlapResults.Contains(sensorCollider);

            return result;
        }

        public static string GetMirroredSensorId(string sensorId)
        {
            if (sensorId == "C")
                return "C";
            var sensorLane = int.Parse(sensorId.Substring(1, 1));
            var sensorName = sensorId.Substring(0, 1);

            sensorLane = sensorLane switch
            {
                1 => 1,
                2 => 8,
                3 => 7,
                4 => 6,
                5 => 5,
                8 => 2,
                7 => 3,
                6 => 4,
                _ => sensorLane
            };

            return sensorName + sensorLane;
        }

        public static string GetUpdatedSensorId(string sensorId, int fromLaneIndex)
        {
            if (sensorId == "C")
                return "C";
            var sensorLane = int.Parse(sensorId.Substring(1, 1));
            var sensorName = sensorId.Substring(0, 1);
            sensorLane += fromLaneIndex;

            if (sensorLane > 8)
                sensorLane -= 8;
            else if (sensorLane < 1)
                sensorLane += 8;

            return sensorName + sensorLane;
        }

        public override void ManualUpdate()
        {
            GetSlideTransform(ref _slideTransform);

            SlideContentRoot.SetActive(_slideTransform.Shown);

            if (!_slideTransform.Shown)
            {
                if (_haveShown) enabled = false;
                return;
            }

            _haveShown = true;

            _materialPropertyBlock.SetFloat("_Transition", _slideTransform.StarAlpha);

            _materialPropertyBlock.SetTexture("_MainTex", _starTexture);

            if (_slideTransform.StarPosition > 0.002 && !_launchSoundPlayed)
            {
                _launchSoundPlayed = true;
                PlayLaunchSound();
            }

            foreach (var star in Stars)
            {
                star.Move(_slideTransform.StarPosition);

                star.GetSpriteRenderer().SetPropertyBlock(_materialPropertyBlock);
                star.GetSpriteRenderer().transform.localScale =
                    Vector3.one + Vector3.one * _slideTransform.StarAlpha / 2;
            }

            foreach (var segment in Segments)
            foreach (var arrowRenderer in segment.slideSpriteRenderers)
                if ((!segment.touched && !Slided && !segment.arrowInBetweenConcealed) ||
                    _slideTransform.ArrowAlpha == 0)
                    arrowRenderer.color = new Color(1, 1, 1, _slideTransform.ArrowAlpha);

            if (ChartPlayer.Instance.TimeInMilliseconds >= _showJudgeDisplayTiming && _showJudgeDisplayTiming != -1 &&
                Slided &&
                !_concealed)
            {
                if (IsBreak)
                    Scoreboard.BreakCount.Count(_judgeState);
                else
                    Scoreboard.SlideCount.Count(_judgeState);

                if (_judgeState is not (JudgeState.CriticalPerfect or JudgeState.Miss))
                {
                    if (_isFast)
                        Scoreboard.FastCount++;
                    else
                        Scoreboard.LateCount++;
                }

                if (_judgeState is not JudgeState.Miss)
                {
                    PlayJudgeSound();
                    Scoreboard.Combo++;
                }
                else
                {
                    Scoreboard.ResetCombo();
                }

                PlayJudgeAnimation();

                _concealed = true;
            }

            if (ChartPlayer.Instance.TimeInMilliseconds >=
                Timing + WaitDuration + SlideDuration +
                ChartPlayer.Instance.slideJudgeSettings.fastGoodTiming + ChartPlayer.Instance.judgeDelay
                && !_concealed && !Slided)
            {
                if (!_slidedHalf)
                {
                    UpdateJudgeDisplayDirection(5);
                    _judgeState = JudgeState.Miss;
                }
                else
                {
                    UpdateJudgeDisplayDirection(4);
                    _judgeState = JudgeState.Good;

                    _isFast = false;
                }

                Slided = true;

                _holdJudgeAction.Enabled = false;
                _leaveJudgeAction.Enabled = false;

                _showJudgeDisplayTiming = (int)(StarInLastSegmentDuration + ChartPlayer.Instance.TimeInMilliseconds);
            }
        }

        private void PlayJudgeAnimation()
        {
            JudgeDisplaySpriteRenderer.enabled = true;
            _judgeDisplayAnimator.SetTrigger("ShowJudgeDisplay");
        }

        protected void PlayLaunchSound()
        {
            if (IsBreak)
                SfxManager.Instance.PlayBreakSlideLaunchingSound();
        }

        protected void PlaySlideSound()
        {
            if (IsBreak)
            {
                SfxManager.Instance.PlayBreakSlideSlideSound();
            }
            else
            {
                SfxManager.Instance.PlaySlideSound();
            }
        }

        protected void PlayJudgeSound()
        {
            if (!IsBreak)
                return;

            switch (_judgeState)
            {
                case JudgeState.CriticalPerfect:
                case JudgeState.Perfect:
                case JudgeState.SemiCriticalPerfect:
                    SfxManager.Instance.PlaySlideBreakPerfectSound();
                    break;
            }
        }

        protected abstract void OnSensorLeave(TouchEventArgs e);

        protected abstract void OnSensorHold(TouchEventArgs e);

        private void OnLeaveSlidePath(object sender, TouchEventArgs e)
        {
            OnSensorLeave(e);
        }

        private void OnHoldSlidePath(object sender, TouchEventArgs e)
        {
            OnSensorHold(e);
        }

        protected void ConcealSegment(int touchedSegmentsIndex, bool sensorJumpedForLastSegment)
        {
            if (touchedSegmentsIndex >= Segments.Length - 2)
                _slidedHalf = true;

            StartCoroutine(DelayedTrigger(() =>
            {
                if (touchedSegmentsIndex - 1 >= 0)
                    if (Segments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea.Length > 0)
                        Segments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea[^1].color =
                            new Color(1, 1, 1, 0);

                var segment = Segments[touchedSegmentsIndex];

                foreach (var slideSprite in segment.slideSpriteRenderers) slideSprite.color = new Color(1, 1, 1, 0);

                if (touchedSegmentsIndex != Segments.Length - 2 && sensorJumpedForLastSegment)
                    segment.slideSpriteRenderersWithinSensorArea[^1].color = new Color(1, 1, 1, 0.5f);
            }));
        }

        private IEnumerator DelayedTrigger(Action callback)
        {
            yield return new WaitForSeconds(ChartPlayer.Instance.slideConcealDelay / 1000f);

            callback?.Invoke();
        }

        protected void ConcealMiddleSegment(int touchedSegmentsIndex)
        {
            StartCoroutine(DelayedTrigger(() =>
            {
                var segment = Segments[touchedSegmentsIndex];
                if (touchedSegmentsIndex - 1 >= 0 &&
                    Segments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea.Length > 0)
                    Segments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea[^1].color =
                        new Color(1, 1, 1, 0);

                foreach (var slideSprite in segment.slideSpriteRenderersOutsideSensorArea)
                    slideSprite.color = new Color(1, 1, 1, 0);

                segment.arrowInBetweenConcealed = true;
            }));
        }

        private void GetSlideTransform(ref SlideTransform result)
        {
            var currentTime = ChartPlayer.Instance.TimeInMilliseconds;

            var startAppearingTime =
                Timing - ChartPlayer.Instance.timeGapBeforeSlideStartsAppearing;

            if (currentTime < startAppearingTime - 100 || currentTime >= Timing + WaitDuration + SlideDuration +
                ChartPlayer.Instance.slideJudgeSettings.lateGoodTiming +
                ChartPlayer.Instance.slideJudgeDisplayAnimationDuration + StarInLastSegmentDuration)
            {
                result.Shown = false;
                return;
            }

            if (currentTime >= _showJudgeDisplayTiming && _showJudgeDisplayTiming != -1 && Slided)
            {
                result.Shown = true;
                result.StarAlpha = 0;
                result.StarPosition = 1;
                result.ArrowAlpha = 0;

                return;
            }

            if (currentTime >= startAppearingTime && currentTime < Timing)
            {
                result.Shown = true;

                var slideFadeInDuration = ChartPlayer.Instance.slideFadeInDuration;

                if (currentTime < 200 + startAppearingTime)
                    result.ArrowAlpha = (currentTime - startAppearingTime) / 200 / 2f;
                else if (currentTime > 200 + startAppearingTime)
                    result.ArrowAlpha = 0.5f;
                else if (startAppearingTime + slideFadeInDuration - currentTime <= 0)
                    result.ArrowAlpha = 1f;

                result.StarAlpha = 0;
                result.StarPosition = 0.001f;
            }
            else if (currentTime >= Timing && currentTime < Timing + WaitDuration)
            {
                result.Shown = true;
                result.StarAlpha = SuddenlyAppears ? 0 : (currentTime - Timing) / WaitDuration;
                result.ArrowAlpha = 1;
                result.StarPosition = 0.001f;
            }
            else if (currentTime >= Timing + WaitDuration && currentTime < Timing + WaitDuration + SlideDuration)
            {
                result.Shown = true;
                result.StarAlpha = 1;
                result.StarPosition = (currentTime - Timing - WaitDuration) / SlideDuration * 0.999f + 0.001f;
                result.ArrowAlpha = 1;
            }
            else
            {
                result.Shown = true;
                result.StarAlpha = 1;
                result.StarPosition = 1;
                result.ArrowAlpha = 1;

                if (currentTime >= Timing + WaitDuration + SlideDuration +
                    ChartPlayer.Instance.slideJudgeSettings.lateGoodTiming)
                {
                    result.StarAlpha = 0;
                    result.StarPosition = 1;
                    result.ArrowAlpha = 0;
                }

                if (currentTime < startAppearingTime)
                {
                    result.Shown = false;
                    result.StarAlpha = 0;
                    result.StarPosition = 0.001f;
                    result.ArrowAlpha = 0;
                }
            }
        }

        [Serializable]
        public class Segment
        {
            public enum Lane
            {
                Left,
                Center,
                Right,
                Single
            }

            public enum SensorType
            {
                Main,
                Alternative
            }

            public Sensor[] sensors;

            public SpriteRenderer[] slideSpriteRenderers;
            [HideInInspector] public SpriteRenderer[] slideSpriteRenderersWithinSensorArea;
            [HideInInspector] public SpriteRenderer[] slideSpriteRenderersOutsideSensorArea;

            public bool tapped;
            public bool touched;
            public bool arrowInBetweenConcealed;

            public bool canBeSkipped = true;

            [Serializable]
            public class Sensor
            {
                public string sensor;
                public Lane lane = Lane.Single;
                public SensorType type = SensorType.Main;
            }
        }

        private class SlideTransform
        {
            public float ArrowAlpha;

            public bool Shown;
            public float StarAlpha;
            public float StarPosition;
        }
    }
}