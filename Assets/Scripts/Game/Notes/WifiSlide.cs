using System;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Notes
{
    public class WifiSlide : SlideBasedNote
    {
        private const int WifiSlideArrowCount = 11;
        [FormerlySerializedAs("stars")] public WifiStarMovementController[] wifiStars;
        public Segment[] wifiSegments;

        public SpriteRenderer judgeDisplaySpriteRenderer;

        public string svgAssetPath;
        public float pathRotation;

        private readonly int[][] _slideArrowGroups =
        {
            new[] { 0, 1 }, new[] { 2, 3, 4 }, new[] { 5, 6, 7 }, new[] { 8, 9, 10 }
        };

        private readonly List<SpriteRenderer> _slideArrowSpriteRenderers = new();

        private IndividualStarMovementController _individualIndividualStarMovementController;

        private string _lastHeldLSensorId = "";
        private string _lastHeldMSensorId = "";
        private string _lastHeldRSensorId = "";

        private bool _lastLSegmentTouchedByHolding;
        private bool _lastMSegmentTouchedByHolding;
        private bool _lastRSegmentTouchedByHolding;

        private bool _sensorJumped;

        private bool _slideStarted;
        private int _touchedLSegmentIndex;
        private int _touchedMSegmentIndex;
        private int _touchedRSegmentIndex;

        private VectorGraphicsUtility _vectorGraphicsUtility;

        public NoteDataObject.IndividualSlideDataObject SlideData { get; set; }

        protected override SpriteRenderer GetJudgeDisplaySpriteRenderer()
        {
            return judgeDisplaySpriteRenderer;
        }

        protected override (int judgeTiming, int starInLastSegmentDuration, Segment[] segments)
            InitializeSlideSegments()
        {
            var lastSegmentDuration = _slideArrowGroups[^1].Length / WifiSlideArrowCount * SlideDuration;

            for (var i = 0; i < wifiSegments.Length; i++)
            {
                var wifiSegment = wifiSegments[i];

                wifiSegment.slideSpriteRenderers =
                    _slideArrowGroups[i].Select(x => _slideArrowSpriteRenderers[x]).ToArray();
                wifiSegment.slideSpriteRenderersWithinSensorArea = wifiSegment.slideSpriteRenderers;
                wifiSegment.slideSpriteRenderersOutsideSensorArea = Array.Empty<SpriteRenderer>();
            }

            return (Timing + WaitDuration + SlideDuration - lastSegmentDuration, lastSegmentDuration, wifiSegments);
        }

        protected override IStarMovementController[] GetStars()
        {
            return wifiStars;
        }

        private void GenerateSlideArrows()
        {
            transform.position = Vector3.zero;

            for (var i = 0; i < WifiSlideArrowCount; i++)
            {
                var division = WifiSlideArrowCount + 1.35;

                var currentProgress = (float)i + 1;

                var progress = (float)(currentProgress / division + (currentProgress - 2) / 30
                                       - (currentProgress - 1) * 0.48f / division);

                var prefab = IsBreak
                    ? NoteGenerator.Instance.breakSlideArrowPrefab
                    : NoteGenerator.Instance.slideArrowPrefab;

                var arrowInstance = Instantiate(prefab, transform);

                var pair = _vectorGraphicsUtility.GetPositionRotationPair(progress, true);
                arrowInstance.transform.position = pair.position;
                arrowInstance.transform.rotation = pair.rotation;

                var slideSpriteRenderer = arrowInstance.GetComponent<SpriteRenderer>();

                slideSpriteRenderer.sprite = (IsEach, IsBreak) switch
                {
                    (_, true) => NoteGenerator.Instance.wifiSlideBreakSprites[i],
                    (true, false) => NoteGenerator.Instance.wifiSlideEachSprites[i],
                    (false, false) => NoteGenerator.Instance.wifiSlideSprites[i]
                };

                slideSpriteRenderer.sortingOrder = 1 + i + Order;

                arrowInstance.transform.eulerAngles = new Vector3(0, 0, 315) +
                                                      arrowInstance.transform
                                                          .parent.eulerAngles;

                _slideArrowSpriteRenderers.Add(slideSpriteRenderer);
            }

            foreach (var starMovementController in wifiStars)
                starMovementController.spriteRenderer.sprite = (IsEach, IsBreak) switch
                {
                    (_, true) => NoteGenerator.Instance.breakStarSprite,
                    (false, false) => NoteGenerator.Instance.starSprite,
                    (true, false) => NoteGenerator.Instance.eachStarSprite
                };
        }

        protected override void UpdateJudgeDisplayDirection(int judgeDisplaySpriteGroupIndex)
        {
            var judgeSpriteNeedsChange =
                JudgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is >= 265 and <= 365 or >= -5 and <= 95;

            JudgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance
                .slideJudgeDisplaySprites[judgeDisplaySpriteGroupIndex]
                .wifiSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? 0
                        : 1];

            if (!judgeSpriteNeedsChange) JudgeDisplaySpriteRenderer.transform.eulerAngles += new Vector3(0, 0, 180);
        }

        protected override int GenerateSlideArrowObjects()
        {
            GenerateSlideArrows();
            return WifiSlideArrowCount;
        }

        protected override void InitializeVectorGraphicsUtility()
        {
            _vectorGraphicsUtility = new VectorGraphicsUtility(svgAssetPath, pathRotation, false,
                Lanes.Instance.endPoints[SlideData.From - 1].position, 180);
        }

        protected override void InitializeSlideSensorIds()
        {
            foreach (var wifiSegment in wifiSegments)
            foreach (var wifiSegmentSensor in wifiSegment.sensors)
                wifiSegmentSensor.sensor =
                    GetUpdatedSensorId(wifiSegmentSensor.sensor, SlideData.From - 1);
        }

        protected override void InitializePathRotation()
        {
            transform.Rotate(new Vector3(0, 0, -45f * (SlideData.From - 1)));

            pathRotation = -45f * (SlideData.From - 1);
        }

        public override void AddAutoPlayKeyFrame()
        {
            foreach (var segment in Segments)
            {
                var index = Segments.ToList().IndexOf(segment);

                if (index == 0)
                    continue;

                float tapTime;
                float leaveTime;

                if (index == Segments.Length - 1)
                {
                    tapTime = JudgeTiming;
                    leaveTime = JudgeTiming;
                }
                else
                {
                    tapTime = index / (float)Segments.Length * SlideDuration + Timing + WaitDuration;
                    leaveTime = (index + 1) / (float)Segments.Length * SlideDuration + Timing + WaitDuration;
                }

                var lList = AutoPlayer.KeyFrameManager.GetKeyFrames(segment.sensors
                    .FirstOrDefault(x => x.lane == Segment.Lane.Left)?.sensor);

                lList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)tapTime));
                lList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, (int)tapTime));
                lList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressUp, (int)leaveTime));
                lList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)leaveTime));

                var mList = AutoPlayer.KeyFrameManager.GetKeyFrames(segment.sensors
                    .FirstOrDefault(x => x.lane == Segment.Lane.Center)?.sensor);

                mList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)tapTime));
                mList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, (int)tapTime));
                mList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressUp, (int)leaveTime));
                mList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)leaveTime));

                var rList = AutoPlayer.KeyFrameManager.GetKeyFrames(segment.sensors
                    .FirstOrDefault(x => x.lane == Segment.Lane.Right)?.sensor);

                rList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)tapTime));
                rList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, (int)tapTime));
                rList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressUp, (int)leaveTime));
                rList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)leaveTime));
            }
        }

        private void CheckAndJudge()
        {
            if (Slided)
                return;

            if (_touchedLSegmentIndex + _touchedMSegmentIndex + _touchedRSegmentIndex == 12)
            {
                ConcealSegment(Segments.Length - 2,
                    false);

                Segments[^2].touched = true;

                Judge();
            }
        }

        private bool SensorContained(int segmentIndex, string sensorId, int pathIndex)
        {
            var leftSensors = Segments[segmentIndex].sensors.Where(x => x.lane == Segment.Lane.Left)
                .Select(x => x.sensor);
            var middleSensors = Segments[segmentIndex].sensors.Where(x => x.lane == Segment.Lane.Center)
                .Select(x => x.sensor);
            var rightSensors = Segments[segmentIndex].sensors.Where(x => x.lane == Segment.Lane.Right)
                .Select(x => x.sensor);

            return pathIndex switch
            {
                0 => leftSensors.Contains(sensorId),
                1 => middleSensors.Contains(sensorId),
                2 => rightSensors.Contains(sensorId),
                _ => false
            };
        }

        protected override void OnSensorHold(TouchEventArgs e)
        {
            if (!Slided)
            {
                var leftSensors = Segments[^1].sensors.Where(x => x.lane == Segment.Lane.Left).Select(x => x.sensor);
                var middleSensors = Segments[^1].sensors.Where(x => x.lane == Segment.Lane.Center)
                    .Select(x => x.sensor);
                var rightSensors = Segments[^1].sensors.Where(x => x.lane == Segment.Lane.Right).Select(x => x.sensor);

                if (leftSensors.Contains(e.SensorId) && _touchedLSegmentIndex == Segments.Length - 1)
                    _touchedLSegmentIndex++;
                if (middleSensors.Contains(e.SensorId) && _touchedMSegmentIndex == Segments.Length - 1)
                    _touchedMSegmentIndex++;
                if (rightSensors.Contains(e.SensorId) && _touchedRSegmentIndex == Segments.Length - 1)
                    _touchedRSegmentIndex++;
            }

            CheckAndJudge();

            ProcessSlideOnSpecificSlidePath(e, 0, true);
            ProcessSlideOnSpecificSlidePath(e, 1, true);
            ProcessSlideOnSpecificSlidePath(e, 2, true);
        }

        protected override void OnSensorLeave(TouchEventArgs e)
        {
            ProcessSlideOnSpecificSlidePath(e, 0, false);
            ProcessSlideOnSpecificSlidePath(e, 1, false);
            ProcessSlideOnSpecificSlidePath(e, 2, false);
        }

        private void ProcessSlideOnSpecificSlidePath(TouchEventArgs e, int pathIndex, bool isOnHold)
        {
            if (Slided)
                return;

            var minimalTouchedSegmentIndex =
                TernaryMinimal(_touchedRSegmentIndex, _touchedLSegmentIndex, _touchedMSegmentIndex);

            var lastSegmentToBeConcealedIndex = minimalTouchedSegmentIndex - 1;

            var lastHeldSensorId = pathIndex switch
            {
                0 => _lastHeldLSensorId,
                1 => _lastHeldMSensorId,
                2 => _lastHeldRSensorId,
                _ => ""
            };

            var touchedSegmentsIndex = pathIndex switch
            {
                0 => _touchedLSegmentIndex,
                1 => _touchedMSegmentIndex,
                2 => _touchedRSegmentIndex,
                _ => -1
            };

            var lastSegmentTouchedByHolding = pathIndex switch
            {
                0 => _lastLSegmentTouchedByHolding,
                1 => _lastMSegmentTouchedByHolding,
                2 => _lastRSegmentTouchedByHolding,
                _ => false
            };

            if (touchedSegmentsIndex == Segments.Length)
                return;

            if (Timing > ChartPlayer.Instance.TimeInMilliseconds + 50)
                return;

            var sensorJumped =
                touchedSegmentsIndex + 1 != Segments.Length &&
                SensorContained(touchedSegmentsIndex + 1, e.SensorId, pathIndex);

            var activated =
                (SensorContained(touchedSegmentsIndex, e.SensorId, pathIndex) || sensorJumped) &&
                touchedSegmentsIndex < Segments.Length;

            if (!activated)
                return;

            if (!_slideStarted)
            {
                PlaySlideSound();
                _slideStarted = true;
            }

            if (isOnHold)
                if (lastHeldSensorId != e.SensorId)
                {
                    if (sensorJumped)
                        _sensorJumped = true;

                    if (_sensorJumped && lastSegmentTouchedByHolding) _sensorJumped = false;

                    if (sensorJumped || touchedSegmentsIndex == 0)
                        switch (pathIndex)
                        {
                            case 0: _lastLSegmentTouchedByHolding = true; break;
                            case 1: _lastMSegmentTouchedByHolding = true; break;
                            case 2: _lastRSegmentTouchedByHolding = true; break;
                        }

                    switch (pathIndex)
                    {
                        case 0: _lastHeldLSensorId = e.SensorId; break;
                        case 1: _lastHeldMSensorId = e.SensorId; break;
                        case 2: _lastHeldRSensorId = e.SensorId; break;
                    }
                }

            if (!isOnHold)
                switch (pathIndex)
                {
                    case 0: _lastLSegmentTouchedByHolding = false; break;
                    case 1: _lastMSegmentTouchedByHolding = false; break;
                    case 2: _lastRSegmentTouchedByHolding = false; break;
                }

            if (sensorJumped && !isOnHold)
                return;

            if (!sensorJumped && isOnHold)
                return;

            switch (pathIndex)
            {
                case 0:
                    _touchedLSegmentIndex++;
                    break;
                case 1:
                    _touchedMSegmentIndex++;
                    break;
                case 2:
                    _touchedRSegmentIndex++;
                    break;
            }

            if (touchedSegmentsIndex == Segments.Length - 1)
                return;

            var segmentToBeConcealedIndex =
                TernaryMinimal(_touchedLSegmentIndex, _touchedMSegmentIndex, _touchedRSegmentIndex) - 1;

            if (segmentToBeConcealedIndex != -1 &&
                segmentToBeConcealedIndex - lastSegmentToBeConcealedIndex > 0)
            {
                ConcealSegment(segmentToBeConcealedIndex, isOnHold ? false : _sensorJumped);
                Segments[segmentToBeConcealedIndex].touched = true;

                if (!isOnHold)
                    _sensorJumped = false;
            }
        }

        private int TernaryMinimal(int a, int b, int c)
        {
            return Math.Min(Math.Min(a, b), c);
        }
    }
}