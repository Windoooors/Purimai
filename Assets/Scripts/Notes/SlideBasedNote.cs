using System;
using System.Collections.Generic;
using System.Linq;
using ChartManagement;
using LitMotion;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Notes
{
    public abstract class SlideBasedNote : MonoBehaviour
    {
        public NoteDataObject.SlideDataObject.SlideType slideType;

        public int fromLaneIndex;
        public int[] toLaneIndexes;
        public int timing;
        public int waitDuration;
        public int slideDuration;

        public bool isEach;
        public int order;

        public bool isWifi;

        protected bool Slided;

        public StarMovementController[] stars;

        public SpriteRenderer[] slideSpriteRenderers;
        
        public SpriteRenderer judgeDisplaySpriteRenderer;

        public int[] slideJudgeDisplaySpriteIndexes;

        protected readonly List<Segment> UniversalSegments = new();

        protected bool IsClockwise;
        
        private bool _concealed;

        private bool _revealed;
        private bool _starMovingStarted;

        private bool _waitingStarted;

        private Animator _judgeDisplayAnimator;
        
        private void Start()
        {
            InitializeSlideDirection();
            InitializeJudgeDisplayDirection();
            InitializeSlideSensorIds();
            UpdateUniversalSegments();
            
            _judgeDisplayAnimator = judgeDisplaySpriteRenderer.GetComponent<Animator>();
            _judgeDisplayAnimator.enabled = true;
            judgeDisplaySpriteRenderer.color = new Color(1, 1, 1, 0);

            isWifi = slideType == NoteDataObject.SlideDataObject.SlideType.Wifi;

            ReplaceEachSlideSprite();

            foreach (var star in stars)
            {
                if (isEach)
                    star.spriteRenderer.sprite = NoteGenerator.Instance.eachStarSprite;
                star.spriteRenderer.color = new Color(0, 0, 0, 0);
                star.transform.localScale = Vector3.zero;
                star.spriteRenderer.sortingOrder += order;
            }

            InitializeSlideSegments();

            transform.position = NoteGenerator.Instance.outOfScreenPosition;
            SimulatedSensor.OnTap += OnTapSlidePath;
            SimulatedSensor.OnHold += OnHoldSlidePath;
        }

        private void Update()
        {
            if (ChartPlayer.Instance.time >= timing + ChartPlayer.Instance.starAppearanceDelay && !_revealed)
            {
                transform.position = Vector3.zero;

                foreach (var segment in UniversalSegments)
                    segment.MotionHandles = segment.slideSpriteRenderers.Select(spriteRenderer =>
                        LMotion.Create(0, 1f, ChartPlayer.Instance.starAppearanceDuration / 1000f)
                            .WithEase(Ease.Linear)
                            .Bind(x => { spriteRenderer.color = new Color(1, 1, 1, x); })).ToArray();

                foreach (var star in stars) star.MoveToStart();

                _revealed = true;
            }

            if (ChartPlayer.Instance.time >= timing && !_waitingStarted)
            {
                _waitingStarted = true;
                foreach (var star in stars)
                    LMotion.Create(0, 1f, waitDuration / 1000f).WithEase(Ease.Linear)
                        .Bind(x =>
                        {
                            star.spriteRenderer.color = new Color(1, 1, 1, x);
                            star.transform.localScale = Vector3.one + 0.5f * new Vector3(x, x, x);
                        });
            }

            if (ChartPlayer.Instance.time >= timing + waitDuration && !_starMovingStarted)
            {
                _starMovingStarted = true;
                foreach (var star in stars)
                {
                    star.duration = slideDuration / 1000f;
                    star.StartMoving();
                }
            }

            if (ChartPlayer.Instance.time >= timing + waitDuration + slideDuration && !_concealed && Slided)
            {
                foreach (var spriteRenderer in slideSpriteRenderers) spriteRenderer.enabled = false;

                foreach (var star in stars)
                {
                    star.StopMoving();
                    star.spriteRenderer.enabled = false;
                }

                //transform.position = NoteGenerator.Instance.outOfScreenPosition;
                
                if (Slided && !_concealed)
                    PlayJudgeAnimation();
                
                _concealed = true;
            }
            
            if (ChartPlayer.Instance.time >= timing + waitDuration + slideDuration + 600 && !_concealed && !Slided)
            {
                foreach (var spriteRenderer in slideSpriteRenderers) spriteRenderer.enabled = false;

                foreach (var star in stars) star.StopMoving();

                transform.position = NoteGenerator.Instance.outOfScreenPosition;
                
                _concealed = true;
            }
        }

        protected void Judge()
        {
            Slided = true;
        }

        protected void PlayJudgeAnimation()
        {
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

        private void OnHoldSlidePath(object sender, SimulatedSensor.TouchEventArgs e)
        {
            ProcessSlideHold(e);
        }

        private void OnTapSlidePath(object sender, SimulatedSensor.TouchEventArgs e)
        {
            ProcessSlideTap(e);
        }

        protected abstract void ProcessSlideHold(SimulatedSensor.TouchEventArgs e,
            bool sensorJumpedForLastSegment = false);

        protected abstract void ProcessSlideTap(SimulatedSensor.TouchEventArgs e,
            bool sensorJumpedForLastSegment = false);

        private void ReplaceEachSlideSprite()
        {
            var i = 0;
            foreach (var slideSpriteRenderer in slideSpriteRenderers)
            {
                if (isEach)
                {
                    if (isWifi)
                    {
                        slideSpriteRenderer.sprite = NoteGenerator.Instance.wifiSlideEachSprites[i];
                        i++;
                    }
                    else
                    {
                        slideSpriteRenderer.sprite = NoteGenerator.Instance.slideEachSprite;
                    }
                }

                slideSpriteRenderer.sortingOrder += order;
            }
        }

        protected void ConcealSegment(int touchedSegmentsIndex, bool sensorJumpedForLastSegment)
        {
            if (touchedSegmentsIndex - 1 >= 0)
                if (UniversalSegments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea.Length > 0)
                    UniversalSegments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea[^1].color =
                        new Color(0, 0, 0, 0);

            var segment = UniversalSegments[touchedSegmentsIndex];

            foreach (var motionHandle in segment.MotionHandles) motionHandle.TryCancel();

            foreach (var slideSprite in segment.slideSpriteRenderers)
                slideSprite.color = new Color(0, 0, 0, 0);

            if (touchedSegmentsIndex != UniversalSegments.Count - 2 && sensorJumpedForLastSegment)
                segment.slideSpriteRenderersWithinSensorArea[^1].color = new Color(1, 1, 1, 0.5f);
        }

        protected void ConcealMiddleSegment(int touchedSegmentsIndex)
        {
            if (touchedSegmentsIndex - 1 >= 0 && UniversalSegments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea.Length > 0)
                UniversalSegments[touchedSegmentsIndex - 1].slideSpriteRenderersWithinSensorArea[^1].color =
                    new Color(0, 0, 0, 0);

            var segment = UniversalSegments[touchedSegmentsIndex];

            foreach (var motionHandle in segment.MotionHandles) motionHandle.TryCancel();

            foreach (var slideSprite in segment.slideSpriteRenderersOutsideSensorArea)
                slideSprite.color = new Color(0, 0, 0, 0);
        }

        private void InitializeSlideSegments()
        {
            var previousMatchedArrowIndex = -1;

            foreach (var segment in UniversalSegments)
            {
                var matchedSensor = SimulatedSensor.Sensors.Find(x => x.sensorId == segment.mainSensor);

                var spriteWithinAreaList = new List<SpriteRenderer>();
                var spriteOutsideAreaList = new List<SpriteRenderer>();

                var startIndex = previousMatchedArrowIndex + 1;

                for (var i = startIndex;
                     i < slideSpriteRenderers.Length &&
                     (!ArrowOverlapsOnSensor(i - 1, matchedSensor.GetComponent<Collider2D>()) ||
                      (ArrowOverlapsOnSensor(i - 1, matchedSensor.GetComponent<Collider2D>())
                       && ArrowOverlapsOnSensor(i, matchedSensor.GetComponent<Collider2D>())));
                     i++)
                {
                    if (ArrowOverlapsOnSensor(i, matchedSensor.GetComponent<Collider2D>()))
                        spriteWithinAreaList.Add(slideSpriteRenderers[i]);
                    else
                        spriteOutsideAreaList.Add(slideSpriteRenderers[i]);

                    previousMatchedArrowIndex = i;
                }

                segment.slideSpriteRenderersWithinSensorArea = spriteWithinAreaList.ToArray();
                segment.slideSpriteRenderersOutsideSensorArea = spriteOutsideAreaList.ToArray();

                var slideSpriteRenderersList = new List<SpriteRenderer>();

                slideSpriteRenderersList.AddRange(segment.slideSpriteRenderersWithinSensorArea);
                slideSpriteRenderersList.AddRange(segment.slideSpriteRenderersOutsideSensorArea);

                segment.slideSpriteRenderers = slideSpriteRenderersList.ToArray();
            }
        }

        private bool ArrowOverlapsOnSensor(int index, Collider2D sensorCollider)
        {
            if (index == -1)
                return false;

            //if (slideType is not NoteDataObject.SlideDataObject.SlideType.Wifi)
                //return sensorCollider.OverlapPoint(slideSpriteRenderers[index].transform.position);

            var colliderAdded = slideSpriteRenderers[index].TryGetComponent<Collider2D>(out var addedCollider);
            if (!colliderAdded)
                addedCollider = slideSpriteRenderers[index].gameObject.AddComponent<BoxCollider2D>();

            addedCollider.enabled = true;

            var overlapResults = new Collider2D[10];

            var filter = new ContactFilter2D();
            filter.SetLayerMask(1 << LayerMask.NameToLayer("Sensors"));

            addedCollider.Overlap(filter, overlapResults);
            
            addedCollider.enabled = false;

            return overlapResults.Contains(sensorCollider);
        }

        protected virtual void InitializeSlideDirection()
        {
            IsClockwise = true;

            slideJudgeDisplaySpriteIndexes = new[] { 0, 1 };
            
            transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));

            var star = stars[0];
            star.pathRotation = -45f * fromLaneIndex;
        }

        protected virtual void InitializeJudgeDisplayDirection()
        {
            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is > 265 and <= 365 or > -5 and <= 95;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance.slideJudgeDisplaySprites[0]
                .normalSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? IsClockwise ? slideJudgeDisplaySpriteIndexes[1] : slideJudgeDisplaySpriteIndexes[0]
                        : IsClockwise
                            ? slideJudgeDisplaySpriteIndexes[0]
                            : slideJudgeDisplaySpriteIndexes[1]];
                
            
            var scale = judgeDisplaySpriteRenderer.gameObject.transform.localScale;
            scale = new Vector3(scale.x,
                judgeSpriteNeedsChange ? 0.5f : -0.5f,
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
    }

    [Serializable]
    public class Segment
    {
        [FormerlySerializedAs("sensor")] public string mainSensor;

        [HideInInspector] public SpriteRenderer[] slideSpriteRenderers;
        [HideInInspector] public SpriteRenderer[] slideSpriteRenderersWithinSensorArea;
        [HideInInspector] public SpriteRenderer[] slideSpriteRenderersOutsideSensorArea;

        public MotionHandle[] MotionHandles;
    }
}