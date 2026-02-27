using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using UI.Result;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Notes
{
    public abstract class SlideBasedNote : NoteBase
    {
        public StarMovementController[] stars;
        public SpriteRenderer judgeDisplaySpriteRenderer;
        public string svgAssetPath;

        //public float slideArrowGenerationProgressOffset;
        //public float slideArrowGenerationDivisionOffset;

        public int slideArrowCount;

        [HideInInspector] public NoteDataObject.SlideDataObject.SlideType slideType;

        [HideInInspector] public int fromLaneIndex;
        [HideInInspector] public int[] toLaneIndexes;
        [HideInInspector] public int timing;
        [HideInInspector] public int waitDuration;
        [HideInInspector] public int slideDuration;

        [HideInInspector] public bool isEach;
        [HideInInspector] public int order;

        [HideInInspector] public bool isWifi;

        [HideInInspector] public bool suddenlyAppears;

        [HideInInspector] public float pathRotation;

        [HideInInspector] public bool flipPathY;

        [FormerlySerializedAs("objectRotationOffset")] [HideInInspector]
        public float starObjectRotationOffset = -18;

        protected readonly List<Segment> UniversalSegments = new();

        private bool _concealed;

        private bool _haveShown;

        private bool _isFast;


        private Animator _judgeDisplayAnimator;
        private JudgeState _judgeState;

        private SpriteRenderer[] _slideArrowSpriteRenderers;

        private bool _slidedHalf;

        private SlideTransform _slideTransform = new();
        private bool _starMovingStarted;

        private bool _waitingStarted;

        protected bool IsClockwise;

        protected GameObject SlideContentRoot;

        protected bool Slided;
        protected int[] SlideJudgeDisplaySpriteIndexes;

        protected int SlideJudgeTiming;

        [HideInInspector] public VectorGraphicsUtility VectorGraphicsUtility;

        private void Start()
        {
            transform.position = Vector3.zero;
            InitializeSlideDirection();

            VectorGraphicsUtility = new VectorGraphicsUtility(svgAssetPath, pathRotation, flipPathY,
                Lanes.Instance.endPoints[fromLaneIndex].position,
                180);

            GenerateSlideArrowSpriteRenderers();
            InitializeSlideSensorIds();
            UpdateUniversalSegments();

            VectorGraphicsUtility.ObjectRotationOffset = starObjectRotationOffset;

            if (slideType is not NoteDataObject.SlideDataObject.SlideType.Wifi)
            {
                var pair = VectorGraphicsUtility.GetPositionRotationPair(1f - 0.6f / slideArrowCount);
                judgeDisplaySpriteRenderer.transform.position = pair.position;
                if ((int)slideType is not 0 and not 1 and not 2)
                {
                    judgeDisplaySpriteRenderer.transform.eulerAngles =
                        pair.rotation.eulerAngles + new Vector3(0, 0, 18);
                }
                else
                {
                    if (IsClockwise)
                    {
                        judgeDisplaySpriteRenderer.transform.eulerAngles =
                            pair.rotation.eulerAngles + new Vector3(0, 0, 37.5f);
                    }
                    else
                    {
                        judgeDisplaySpriteRenderer.transform.eulerAngles =
                            pair.rotation.eulerAngles + new Vector3(0, 0, -1.5f);
                        judgeDisplaySpriteRenderer.transform.Rotate(180, 0, 0, Space.Self);
                        //judgeDisplaySpriteRenderer.flipY = true;
                    }
                }
            }

            _judgeDisplayAnimator = judgeDisplaySpriteRenderer.GetComponent<Animator>();
            _judgeDisplayAnimator.enabled = true;
            judgeDisplaySpriteRenderer.color = new Color(1, 1, 1, 0);

            isWifi = slideType == NoteDataObject.SlideDataObject.SlideType.Wifi;

            ReplaceEachSlideSprite();

            foreach (var star in stars)
            {
                if (isEach)
                    star.spriteRenderer.sprite = NoteGenerator.Instance.eachStarSprite;
                star.spriteRenderer.color = new Color(1, 1, 1, 0);
                star.transform.localScale = Vector3.zero;
                star.spriteRenderer.sortingOrder -= order;
            }

            InitializeSlideSegments();

            foreach (var star in stars) star.Initialize();

            judgeDisplaySpriteRenderer.enabled = false;

            judgeDisplaySpriteRenderer.sortingOrder -= order;

            SimulatedSensor.OnHold += OnHoldSlidePath;
            SimulatedSensor.OnLeave += OnLeaveSlidePath;

            Scoreboard.SlideCount.TotalCount++;

            SlideContentRoot = new GameObject("SlideContent");
            SlideContentRoot.transform.SetParent(transform);

            var children = transform.GetComponentsInChildren<Transform>();

            foreach (var child in children) child.parent = SlideContentRoot.transform;

            SetActive(false, SlideContentRoot);

            emergingTime = timing - (suddenlyAppears ? 0 : ChartPlayer.Instance.timeGapBeforeSlideStartsAppearing);
        }

        public override void ManualUpdate()
        {
            GetSlideTransform(ref _slideTransform);

            SetActive(_slideTransform.Shown, SlideContentRoot);

            if (!_slideTransform.Shown)
            {
                if (_haveShown)
                    enabled = false;
                return;
            }

            _haveShown = true;

            foreach (var star in stars)
            {
                star.Move(_slideTransform.StarPosition);
                star.spriteRenderer.color = new Color(1, 1, 1, _slideTransform.StarAlpha);
                star.transform.localScale = Vector3.one + Vector3.one * _slideTransform.StarAlpha / 2;
            }

            foreach (var segment in UniversalSegments)
            foreach (var arrowRenderer in segment.slideSpriteRenderers)
                if ((!segment.touched && !Slided && !segment.arrowInBetweenConcealed) ||
                    _slideTransform.ArrowAlpha == 0)
                    arrowRenderer.color = new Color(1, 1, 1, _slideTransform.ArrowAlpha);

            if (ChartPlayer.Instance.TimeInMilliseconds >= timing + waitDuration + slideDuration && Slided &&
                !_concealed)
            {
                Scoreboard.SlideCount.Count(_judgeState);

                if (_judgeState is not (JudgeState.CriticalPerfect or JudgeState.Miss))
                {
                    if (_isFast)
                        Scoreboard.FastCount++;
                    else
                        Scoreboard.LateCount++;
                }

                Scoreboard.Combo++;

                PlayJudgeAnimation();

                _concealed = true;
            }

            if (ChartPlayer.Instance.TimeInMilliseconds >=
                timing + waitDuration + slideDuration +
                ChartPlayer.Instance.slideJudgeSettings.fastGoodTiming + ChartPlayer.Instance.judgeDelay
                && !_concealed && !Slided)
            {
                if (!_slidedHalf)
                {
                    UpdateJudgeDisplayDirection(5);
                    _judgeState = JudgeState.Miss;

                    Scoreboard.SlideCount.Count(JudgeState.Miss);
                    Scoreboard.ResetCombo();
                }
                else
                {
                    UpdateJudgeDisplayDirection(4);
                    _judgeState = JudgeState.Good;

                    Scoreboard.SlideCount.Count(JudgeState.Good);
                    Scoreboard.LateCount++;
                    Scoreboard.Combo++;
                }

                Slided = true;
                if (!_concealed)
                    PlayJudgeAnimation();

                _concealed = true;
            }
        }

        private void GetSlideTransform(ref SlideTransform result)
        {
            var currentTime = ChartPlayer.Instance.TimeInMilliseconds;

            var startAppearingTime =
                timing - (suddenlyAppears ? 0 : ChartPlayer.Instance.timeGapBeforeSlideStartsAppearing);

            if (currentTime < startAppearingTime - 100 || currentTime >= timing + waitDuration + slideDuration +
                ChartPlayer.Instance.slideJudgeSettings.lateGoodTiming +
                ChartPlayer.Instance.slideJudgeDisplayAnimationDuration)
            {
                result.Shown = false;
                return;
            }

            if (currentTime >= startAppearingTime && currentTime < timing)
            {
                result.Shown = true;

                var slideFadeInDuration = suddenlyAppears ? 0 : ChartPlayer.Instance.slideFadeInDuration;

                if (currentTime < 200 + startAppearingTime)
                    result.ArrowAlpha = (currentTime - startAppearingTime) / 200 / 2f;
                else if (currentTime > 200 + startAppearingTime)
                    result.ArrowAlpha = 0.5f;
                else if (startAppearingTime + slideFadeInDuration - currentTime <= 0)
                    result.ArrowAlpha = 1f;

                result.StarAlpha = 0;
                result.StarPosition = 0;
            }
            else if (currentTime >= timing && currentTime < timing + waitDuration)
            {
                result.Shown = true;
                result.StarAlpha = (currentTime - timing) / waitDuration;
                result.ArrowAlpha = 1;
                result.StarPosition = 0;
            }
            else if (currentTime >= timing + waitDuration && currentTime < timing + waitDuration + slideDuration)
            {
                result.Shown = true;
                result.StarAlpha = 1;
                result.StarPosition = (currentTime - timing - waitDuration) / slideDuration;
                result.ArrowAlpha = 1;
            }
            else
            {
                result.Shown = true;
                result.StarAlpha = 1;
                result.StarPosition = 1;
                result.ArrowAlpha = 1;

                if (currentTime >= timing + waitDuration + slideDuration +
                    ChartPlayer.Instance.slideJudgeSettings.lateGoodTiming)
                {
                    result.StarAlpha = 0;
                    result.StarPosition = 1;
                    result.ArrowAlpha = 0;
                }

                if (currentTime >= timing + waitDuration + slideDuration && Slided)
                {
                    result.StarAlpha = 0;
                    result.StarPosition = 1;
                    result.ArrowAlpha = 0;
                }

                if (currentTime < startAppearingTime)
                {
                    result.Shown = false;
                    result.StarAlpha = 0;
                    result.StarPosition = 0;
                    result.ArrowAlpha = 0;
                }
            }
        }

        private void GenerateSlideArrowSpriteRenderers()
        {
            var slideArrowList = new List<GameObject>();

            VectorGraphicsUtility.SetStartPosition(Lanes.Instance.endPoints[fromLaneIndex].position);

            var slideArrowOrder = 0;

            for (var i = 1; i <= slideArrowCount; i++)
            {
                var division = slideArrowCount + 1.35;

                var currentProgress = (float)i;
                if (slideType is NoteDataObject.SlideDataObject.SlideType.Line)
                {
                    division -= 0.45f;
                    currentProgress -= 0.65f;
                }

                if ((int)slideType is 0 or 1 or 2)
                {
                    division -= 1.33f;
                    currentProgress -= 0.60f;
                }

                var progress = currentProgress / division;

                if (isWifi)
                    progress += (currentProgress - 2) / 30
                                - (currentProgress - 1) * 0.48f / division;

                var pair = VectorGraphicsUtility.GetPositionRotationPair((float)progress);

                var arrowObject = Instantiate(NoteGenerator.Instance.slideArrowPrefab, transform);

                arrowObject.GetComponent<SpriteRenderer>().sortingOrder = slideArrowOrder--;

                arrowObject.transform.position = pair.Item1;
                arrowObject.transform.rotation = pair.Item2;

                if (isWifi)
                    arrowObject.transform.eulerAngles = new Vector3(0, 0, 315) +
                                                        arrowObject.transform
                                                            .parent.eulerAngles;

                slideArrowList.Add(arrowObject);
            }

            _slideArrowSpriteRenderers = slideArrowList.Select(x => x.GetComponent<SpriteRenderer>()).ToArray();
        }

        protected void Judge()
        {
            if (Slided)
                return;

            var deltaTiming = SlideJudgeTiming - ChartPlayer.Instance.TimeInMilliseconds +
                              ChartPlayer.Instance.judgeDelay;

            _isFast = deltaTiming > 0;

            var absDeltaTiming = math.abs(deltaTiming);

            var judgeSettings = ChartPlayer.Instance.slideJudgeSettings;

            if (deltaTiming < -judgeSettings.lateGoodTiming)
                return;

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
        }

        private void PlayJudgeAnimation()
        {
            judgeDisplaySpriteRenderer.enabled = true;
            _judgeDisplayAnimator.SetTrigger("ShowJudgeDisplay");
        }

        protected string GetUpdatedSensorId(string sensorId)
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

        private void OnLeaveSlidePath(object sender, TouchEventArgs e)
        {
            OnSensorLeave(e);
        }

        private void OnHoldSlidePath(object sender, TouchEventArgs e)
        {
            OnSensorHold(e);
        }

        protected abstract void OnSensorLeave(TouchEventArgs e);

        protected abstract void OnSensorHold(TouchEventArgs e);

        private void ReplaceEachSlideSprite()
        {
            var i = 0;
            foreach (var slideSpriteRenderer in _slideArrowSpriteRenderers)
            {
                if (isWifi)
                    slideSpriteRenderer.sprite = isEach
                        ? NoteGenerator.Instance.wifiSlideEachSprites[i]
                        : NoteGenerator.Instance.wifiSlideSprites[i];
                else
                    slideSpriteRenderer.sprite =
                        isEach ? NoteGenerator.Instance.slideEachSprite : NoteGenerator.Instance.slideSprite;

                i++;

                slideSpriteRenderer.sortingOrder += order;
            }
        }

        protected void ConcealSegment(int touchedSegmentsIndex, bool sensorJumpedForLastSegment)
        {
            if (touchedSegmentsIndex >= UniversalSegments.Count - 2)
                _slidedHalf = true;

            StartCoroutine(DelayedTrigger(() =>
            {
                if (touchedSegmentsIndex - 1 >= 0)
                    if (UniversalSegments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea.Length > 0)
                        UniversalSegments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea[^1].color =
                            new Color(1, 1, 1, 0);

                var segment = UniversalSegments[touchedSegmentsIndex];

                foreach (var slideSprite in segment.slideSpriteRenderers) slideSprite.color = new Color(1, 1, 1, 0);

                if (touchedSegmentsIndex != UniversalSegments.Count - 2 && sensorJumpedForLastSegment)
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
                var segment = UniversalSegments[touchedSegmentsIndex];
                if (touchedSegmentsIndex - 1 >= 0 &&
                    UniversalSegments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea.Length > 0)
                    UniversalSegments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea[^1].color =
                        new Color(1, 1, 1, 0);

                foreach (var slideSprite in segment.slideSpriteRenderersOutsideSensorArea)
                    slideSprite.color = new Color(1, 1, 1, 0);

                segment.arrowInBetweenConcealed = true;
            }));
        }

        private void InitializeSlideSegments()
        {
            var previousMatchedArrowIndex = -1;

            foreach (var segment in UniversalSegments)
            {
                if (segment.slideSpriteRenderersOutsideSensorArea.Length > 0 ||
                    segment.slideSpriteRenderersWithinSensorArea.Length > 0)
                {
                    var slideSpriteRendererList = new List<SpriteRenderer>();
                    slideSpriteRendererList.AddRange(segment.slideSpriteRenderersOutsideSensorArea);
                    slideSpriteRendererList.AddRange(segment.slideSpriteRenderersWithinSensorArea);
                    segment.slideSpriteRenderers = slideSpriteRendererList.ToArray();
                    continue;
                }

                var matchedSensorShape = SensorShape.SensorShapes.Find(x => x.sensorId == segment.mainSensor);

                var spriteWithinAreaList = new List<SpriteRenderer>();
                var spriteOutsideAreaList = new List<SpriteRenderer>();

                var startIndex = previousMatchedArrowIndex + 1;

                for (var i = startIndex;
                     i < _slideArrowSpriteRenderers.Length &&
                     (!ArrowOverlapsOnSensor(i - 1, matchedSensorShape.GetComponent<Collider2D>()) ||
                      (ArrowOverlapsOnSensor(i - 1, matchedSensorShape.GetComponent<Collider2D>())
                       && ArrowOverlapsOnSensor(i, matchedSensorShape.GetComponent<Collider2D>())));
                     i++)
                {
                    if (ArrowOverlapsOnSensor(i, matchedSensorShape.GetComponent<Collider2D>()))
                        spriteWithinAreaList.Add(_slideArrowSpriteRenderers[i]);
                    else
                        spriteOutsideAreaList.Add(_slideArrowSpriteRenderers[i]);

                    previousMatchedArrowIndex = i;
                }

                segment.slideSpriteRenderersWithinSensorArea = spriteWithinAreaList.ToArray();
                segment.slideSpriteRenderersOutsideSensorArea = spriteOutsideAreaList.ToArray();

                var slideSpriteRenderersList = new List<SpriteRenderer>();

                slideSpriteRenderersList.AddRange(segment.slideSpriteRenderersWithinSensorArea);
                slideSpriteRenderersList.AddRange(segment.slideSpriteRenderersOutsideSensorArea);

                segment.slideSpriteRenderers = slideSpriteRenderersList.ToArray();
            }

            var lastSegmentArrowCount = UniversalSegments[^1].slideSpriteRenderersWithinSensorArea.Length;
            SlideJudgeTiming = (int)((float)(_slideArrowSpriteRenderers.Length - lastSegmentArrowCount) /
                                     _slideArrowSpriteRenderers.Length * slideDuration
                                     + timing + waitDuration);
        }

        private bool ArrowOverlapsOnSensor(int index, Collider2D sensorCollider)
        {
            if (index == -1)
                return false;

            if (slideType is NoteDataObject.SlideDataObject.SlideType.RotateLeft
                or NoteDataObject.SlideDataObject.SlideType.RotateRight
                or NoteDataObject.SlideDataObject.SlideType.RotateMinorArc)
            {
                var pointResult = sensorCollider.OverlapPoint(_slideArrowSpriteRenderers[index].transform.position);
                return pointResult;
            }

            var colliderAdded = _slideArrowSpriteRenderers[index].TryGetComponent<BoxCollider2D>(out var addedCollider);
            if (!colliderAdded)
                addedCollider = _slideArrowSpriteRenderers[index].gameObject.AddComponent<BoxCollider2D>();

            addedCollider.enabled = true;

            var overlapResults = new List<Collider2D>();

            var filter = new ContactFilter2D();
            filter.SetLayerMask(LayerMask.GetMask("Sensors"));

            addedCollider.Overlap(filter, overlapResults);

            Destroy(addedCollider);

            var result = overlapResults.Contains(sensorCollider);

            return result;
        }

        protected virtual void InitializeSlideDirection()
        {
            IsClockwise = true;

            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));

            pathRotation = -45f * fromLaneIndex;
        }

        protected void PlaySlideSound()
        {
            AudioManager.Instance.PlaySlideSound();
        }

        protected virtual void UpdateJudgeDisplayDirection(int judgeSpriteGroupIndex)
        {
            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is > 265 and <= 365 or > -5 and <= 95;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance
                .slideJudgeDisplaySprites[judgeSpriteGroupIndex]
                .normalSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? SlideJudgeDisplaySpriteIndexes[1]
                        : SlideJudgeDisplaySpriteIndexes[0]];

            var scale = judgeDisplaySpriteRenderer.gameObject.transform.localScale;
            scale = new Vector3(scale.x,
                judgeSpriteNeedsChange ? Mathf.Abs(scale.y) : -Mathf.Abs(scale.y),
                scale.z);
            judgeDisplaySpriteRenderer.transform.localScale = scale;
        }

        protected string GetMirroredSensorId(string sensorId)
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

        protected abstract void InitializeSlideSensorIds();
        protected abstract void UpdateUniversalSegments();

        public static int GetShortestInterval(int fromLane, int toLane)
        {
            if (fromLane == toLane) return 0;

            var clockwiseInterval = (toLane - fromLane + 8) % 8;
            var counterClockwiseInterval = (fromLane - toLane + 8) % 8;

            return math.min(clockwiseInterval, counterClockwiseInterval);
        }

        public static (int clockwiseInterval, int counterClockwiseInterval) GetIntervalInBothWays(int start, int end)
        {
            var clockwise = (end - start + 8) % 8;

            var counterClockwise = (start - end + 8) % 8;

            return (clockwise, counterClockwise);
        }

        private class SlideTransform
        {
            public float ArrowAlpha;

            public bool Shown;
            public float StarAlpha;
            public float StarPosition;
        }
    }

    [Serializable]
    public class Segment
    {
        [FormerlySerializedAs("sensor")] public string mainSensor;

        [HideInInspector] public SpriteRenderer[] slideSpriteRenderers;
        public SpriteRenderer[] slideSpriteRenderersWithinSensorArea;
        public SpriteRenderer[] slideSpriteRenderersOutsideSensorArea;

        public bool touched;
        public bool arrowInBetweenConcealed;
    }
}