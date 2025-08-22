using System;
using System.Collections;
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

        public StarMovementController[] stars;

        public SpriteRenderer[] slideSpriteRenderers;

        protected readonly List<Segment> UniversalSegments = new();
        private bool _concealed;

        private bool _revealed;
        private bool _starMovingStarted;

        private bool _waitingStarted;

        private void Start()
        {
            InitializeSlideDirection();
            InitializeSlideSensorIds();
            UpdateUniversalSegments();

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
            SimulatedSensor.OnHold += OnTouchSlidePath;
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
                            star.spriteRenderer.color = new Color(1, 1, 1, 0.5f + 0.5f * x);
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

            if (ChartPlayer.Instance.time >= timing + waitDuration + slideDuration + 100 && !_concealed)
            {
                //foreach (var spriteRenderer in slideSpriteRenderers) spriteRenderer.enabled = false;

                foreach (var star in stars) star.StopMoving();

                transform.position = NoteGenerator.Instance.outOfScreenPosition;

                _concealed = true;
            }
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

        private void OnTouchSlidePath(object sender, SimulatedSensor.TouchEventArgs e)
        {
            ProcessSlideTouch(e);
        }

        protected abstract void ProcessSlideTouch(SimulatedSensor.TouchEventArgs e,
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

        protected IEnumerator ConcealSegment(int touchedSegmentsIndex, bool sensorJumpedForLastSegment, float delay = 0.075f)
        {
            yield return new WaitForSeconds(delay);
            
            if (touchedSegmentsIndex - 1 >= 0)
                UniversalSegments[touchedSegmentsIndex - 1].slideSpriteRenderers[^1].color = new Color(0, 0, 0, 0);
            
            var segment = UniversalSegments[touchedSegmentsIndex];
            
            foreach (var motionHandle in segment.MotionHandles) motionHandle.TryCancel();

            foreach (var slideSprite in segment.slideSpriteRenderers) slideSprite.color = new Color(0, 0, 0, 0);

            if (touchedSegmentsIndex != UniversalSegments.Count - 2 && sensorJumpedForLastSegment)
                segment.slideSpriteRenderers[^1].color = new Color(1, 1, 1, 0.5f);
        }

        private void InitializeSlideSegments()
        {
            var previousMatchedSlideIndex = 0;

            for (var j = 0; j < UniversalSegments.Count; j++)
            {
                var segment = UniversalSegments[j];
                var nextSegment = UniversalSegments[j + 1 == UniversalSegments.Count ? j : j + 1];

                var nextMatchedSensor = SimulatedSensor.Sensors.Find(x => x.sensorId == nextSegment.mainSensor);

                var spriteList = new List<SpriteRenderer>();

                var nextMatchedSlideIndex = 0;

                if (j + 1 != UniversalSegments.Count)
                    for (var l = previousMatchedSlideIndex; l < slideSpriteRenderers.Length; l++)
                    {
                        var nextSlideSpriteRenderer = slideSpriteRenderers[l];

                        var overlapResults = new Collider2D[10];

                        var nextSlideCollider = nextSlideSpriteRenderer.gameObject.AddComponent<BoxCollider2D>();

                        nextMatchedSensor.GetComponent<Collider2D>()
                            .Overlap(new ContactFilter2D().NoFilter(), overlapResults);

                        if (!overlapResults.Contains(nextSlideCollider))
                        {
                            nextSlideCollider.enabled = false;
                            continue;
                        }

                        nextSlideCollider.enabled = false;
                        nextMatchedSlideIndex = l;
                        break;
                    }

                for (var k = previousMatchedSlideIndex;
                     k < (nextMatchedSlideIndex == 0 ? slideSpriteRenderers.Length : nextMatchedSlideIndex);
                     k++)
                {
                    var slideSpriteRenderer = slideSpriteRenderers[k];

                    spriteList.Add(slideSpriteRenderer);
                    previousMatchedSlideIndex = k + 1;
                }

                segment.slideSpriteRenderers = spriteList.ToArray();
            }
        }

        protected virtual void InitializeSlideDirection()
        {
            transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));

            var star = stars[0];
            star.pathRotation = -45f * fromLaneIndex;
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
        public MotionHandle[] MotionHandles;
    }
}