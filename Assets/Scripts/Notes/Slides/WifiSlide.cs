using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Notes.Slides
{
    public class WifiSlide : SlideBasedNote
    {
        public WifiSegment[] segments;
        private int _touchedLSegmentsIndex;
        private int _touchedMSegmentsIndex;

        private int _touchedRSegmentsIndex;

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

        protected override void ProcessSlideTap(SimulatedSensor.TouchEventArgs e,
            bool sensorJumpedForLastSegment = false)
        {
        }

        protected override void ProcessSlideHold(SimulatedSensor.TouchEventArgs e,
            bool sensorJumpedForLastSegment = false)
        {
            ProcessSlideHoldOnSpecificSlidePath(e, 0);
            ProcessSlideHoldOnSpecificSlidePath(e, 1);
            ProcessSlideHoldOnSpecificSlidePath(e, 2);
        }

        private void ProcessSlideHoldOnSpecificSlidePath(SimulatedSensor.TouchEventArgs e, int pathIndex,
            bool sensorJumpedForLastSegment = false)
        {
            var touchedSegmentsIndex = pathIndex switch
            {
                0 => _touchedLSegmentsIndex,
                1 => _touchedMSegmentsIndex,
                2 => _touchedRSegmentsIndex,
                _ => -1
            };

            if (timing - 100 > ChartPlayer.Instance.time)
                return;

            if (touchedSegmentsIndex == segments.Length - 1 || touchedSegmentsIndex == segments.Length)
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

            var segmentToBeConcealedIndex = math.min(_touchedRSegmentsIndex,
                math.min(_touchedLSegmentsIndex, _touchedMSegmentsIndex)) - 1;

            if (segmentToBeConcealedIndex != -1)
                ConcealSegment(segmentToBeConcealedIndex, sensorJumpedForLastSegment);


            if (sensorJumped)
                ProcessSlideHoldOnSpecificSlidePath(e, pathIndex, true);
        }

        protected override void InitializeSlideDirection()
        {
            transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));

            foreach (var star in stars) star.pathRotation = -45f * fromLaneIndex;
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