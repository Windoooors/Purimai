using System;
using System.Linq;
using UnityEngine;

namespace Notes.Slides
{
    public class WifiSlide : SlideBasedNote
    {
        public WifiSegment[] segments;

        private string _lastHeldLSensorId = "";
        private string _lastHeldMSensorId = "";
        private string _lastHeldRSensorId = "";

        private bool _lastLSegmentTouchedByHolding;
        private bool _lastMSegmentTouchedByHolding;
        private bool _lastRSegmentTouchedByHolding;

        private bool _sensorJumped;
        private int _touchedLSegmentIndex;
        private int _touchedMSegmentIndex;
        private int _touchedRSegmentIndex;

        protected override void UpdateUniversalSegments()
        {
            UniversalSegments.AddRange(segments);
        }

        private bool SensorContained(int segmentIndex, string sensorId, int pathIndex)
        {
            return pathIndex switch
            {
                0 => segments[segmentIndex].sensorsL.Contains(sensorId),
                1 => segments[segmentIndex].sensorsM.Contains(sensorId),
                2 => segments[segmentIndex].sensorsR.Contains(sensorId),
                _ => false
            };
        }

        protected override void InitializeSlideSensorIds()
        {
            foreach (var segment in segments)
            {
                segment.mainSensor = GetUpdatedSensorId(segment.mainSensor);
                for (var i = 0; i < segment.sensorsL.Length; i++)
                    segment.sensorsL[i] = GetUpdatedSensorId(segment.sensorsL[i]);
                for (var i = 0; i < segment.sensorsR.Length; i++)
                    segment.sensorsR[i] = GetUpdatedSensorId(segment.sensorsR[i]);
                for (var i = 0; i < segment.sensorsM.Length; i++)
                    segment.sensorsM[i] = GetUpdatedSensorId(segment.sensorsM[i]);
            }
        }

        protected override void OnSensorHold(TouchEventArgs e)
        {
            if (!Slided)
            {
                if (segments[^1].sensorsL.Contains(e.SensorId) && _touchedLSegmentIndex == segments.Length - 1)
                    _touchedLSegmentIndex++;
                if (segments[^1].sensorsM.Contains(e.SensorId) && _touchedMSegmentIndex == segments.Length - 1)
                    _touchedMSegmentIndex++;
                if (segments[^1].sensorsR.Contains(e.SensorId) && _touchedRSegmentIndex == segments.Length - 1)
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

            if (touchedSegmentsIndex == segments.Length)
                return;

            if (timing > ChartPlayer.Instance.time)
                return;

            var sensorJumped =
                touchedSegmentsIndex + 1 != segments.Length &&
                SensorContained(touchedSegmentsIndex + 1, e.SensorId, pathIndex);

            var activated =
                (SensorContained(touchedSegmentsIndex, e.SensorId, pathIndex) || sensorJumped) &&
                touchedSegmentsIndex < segments.Length;

            if (!activated)
                return;

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

            if (touchedSegmentsIndex == segments.Length - 1)
                return;

            var segmentToBeConcealedIndex =
                TernaryMinimal(_touchedLSegmentIndex, _touchedMSegmentIndex, _touchedRSegmentIndex) - 1;

            if (segmentToBeConcealedIndex != -1 &&
                segmentToBeConcealedIndex - lastSegmentToBeConcealedIndex > 0)
            {
                ConcealSegment(segmentToBeConcealedIndex, isOnHold ? false : _sensorJumped);
                if (!isOnHold)
                    _sensorJumped = false;
            }
        }

        protected override void InitializeSlideDirection()
        {
            transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));

            foreach (var star in stars) star.pathRotation = -45f * fromLaneIndex;
        }

        protected override void UpdateJudgeDisplayDirection(int judgeDisplaySpriteGroupIndex)
        {
            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is >= 265 and <= 365 or >= -5 and <= 95;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance
                .slideJudgeDisplaySprites[judgeDisplaySpriteGroupIndex]
                .wifiSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? 0
                        : 1];
        }

        private void CheckAndJudge()
        {
            if (Slided)
                return;

            if (_touchedLSegmentIndex + _touchedMSegmentIndex + _touchedRSegmentIndex == 12)
            {
                ConcealSegment(segments.Length - 2,
                    false);
                Judge();
            }
        }

        private int TernaryMinimal(int a, int b, int c)
        {
            return Math.Min(Math.Min(a, b), c);
        }
    }

    [Serializable]
    public class WifiSegment : Segment
    {
        public string[] sensorsL;
        public string[] sensorsM;
        public string[] sensorsR;
    }
}