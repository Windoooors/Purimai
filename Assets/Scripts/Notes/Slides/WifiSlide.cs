using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Notes.Slides
{
    public class WifiSlide : SlideBasedNote
    {
        public WifiSegment[] segments;
        public int _touchedLSegmentsIndex;
        public int _touchedMSegmentsIndex;

        public int _touchedRSegmentsIndex;

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

        protected override void OnSensorTap(TouchEventArgs e)
        {
            if (!e.AllowSensorOverlapping)
                return;
            
            ProcessSlideTapOnSpecificSlidePath(e, 0);
            ProcessSlideTapOnSpecificSlidePath(e, 1);
            ProcessSlideTapOnSpecificSlidePath(e, 2);
            
            if (Slided)
                return;
            
            if (!(segments[^1].sensorsL.Contains(e.SensorId) ||
                  segments[^1].sensorsM.Contains(e.SensorId) ||
                  segments[^1].sensorsR.Contains(e.SensorId)))
                return;

            var minimalIndex = math.min(_touchedRSegmentsIndex,
                math.min(_touchedLSegmentsIndex, _touchedMSegmentsIndex));

            if (minimalIndex == segments.Length) Judge();
        }

        protected override void OnSensorTapDelayed(TouchEventArgs e)
        {
            OnSensorTap(e);
        }

        protected override void OnSensorHold(TouchEventArgs e,
            bool sensorJumpedForLastSegment = false)
        {
            if (!e.AllowSensorOverlapping)
                return;

            ProcessSlideHoldOnSpecificSlidePath(e, 0);
            ProcessSlideHoldOnSpecificSlidePath(e, 1);
            ProcessSlideHoldOnSpecificSlidePath(e, 2);
        }

        private void ProcessSlideTapOnSpecificSlidePath(TouchEventArgs e, int pathIndex)
        {
            var minimalIndex = math.min(_touchedRSegmentsIndex,
                math.min(_touchedLSegmentsIndex, _touchedMSegmentsIndex));

            if (minimalIndex == segments.Length - 1)
                ProcessSlideHoldOnSpecificSlidePath(e, pathIndex);
        }

        private void ProcessSlideHoldOnSpecificSlidePath(TouchEventArgs e, int pathIndex,
            bool sensorJumpedForLastSegment = false)
        {
            if (math.min(_touchedRSegmentsIndex, math.min(_touchedLSegmentsIndex, _touchedMSegmentsIndex)) ==
                segments.Length)
                return;

            var lastSegmentToBeConcealedIndex = math.min(_touchedRSegmentsIndex,
                math.min(_touchedLSegmentsIndex, _touchedMSegmentsIndex)) - 1;

            var touchedSegmentsIndex = pathIndex switch
            {
                0 => _touchedLSegmentsIndex,
                1 => _touchedMSegmentsIndex,
                2 => _touchedRSegmentsIndex,
                _ => -1
            };
            
            if (Slided)
                return;
            
            if (!(segments[^1].sensorsL.Contains(e.SensorId) ||
                  segments[^1].sensorsM.Contains(e.SensorId) ||
                  segments[^1].sensorsR.Contains(e.SensorId)))
                return;

            var minimalIndex = math.min(_touchedRSegmentsIndex,
                math.min(_touchedLSegmentsIndex, _touchedMSegmentsIndex));

            if (minimalIndex == segments.Length) Judge();

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

            switch (pathIndex)
            {
                case 0:
                    _touchedLSegmentsIndex++;
                    break;
                case 1:
                    _touchedMSegmentsIndex++;
                    break;
                case 2:
                    _touchedRSegmentsIndex++;
                    break;
            }

            if (touchedSegmentsIndex == segments.Length - 1)
                return;

            var segmentToBeConcealedIndex = math.min(_touchedRSegmentsIndex,
                math.min(_touchedLSegmentsIndex, _touchedMSegmentsIndex)) - 1;

            if (segmentToBeConcealedIndex != -1 && segmentToBeConcealedIndex - lastSegmentToBeConcealedIndex > 0)
                ConcealSegment(segmentToBeConcealedIndex, sensorJumpedForLastSegment);

            if (sensorJumped)
                ProcessSlideHoldOnSpecificSlidePath(e, pathIndex, true);
        }

        protected override void InitializeSlideDirection()
        {
            transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));

            foreach (var star in stars) star.pathRotation = -45f * fromLaneIndex;
        }

        protected override void InitializeJudgeDisplayDirection()
        {
            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is >= 265 and <= 365 or >= -5 and <= 95;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance.slideJudgeDisplaySprites[0]
                .wifiSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? 0
                        : 1];
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