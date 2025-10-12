using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Notes.Slides
{
    public class WifiSlide : SlideBasedNote
    {
        public WifiSegment[] segments;
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

        protected override void OnSensorTap(TouchEventArgs e)
        {
            CheckAndJudge(e);
        }

        protected override void OnSensorHold(TouchEventArgs e,
            bool sensorJumpedForLastSegment = false)
        {
            ProcessSlideHoldOnSpecificSlidePath(e, 0);
            ProcessSlideHoldOnSpecificSlidePath(e, 1);
            ProcessSlideHoldOnSpecificSlidePath(e, 2);
        }

        private void ProcessSlideHoldOnSpecificSlidePath(TouchEventArgs e, int pathIndex,
            bool sensorJumpedForLastSegment = false)
        {
            CheckAndJudge(e);

            if (Slided)
                return;

            var minimalTouchedSegmentIndex =
                TernaryMinimal(_touchedRSegmentIndex, _touchedLSegmentIndex, _touchedMSegmentIndex);
            if (minimalTouchedSegmentIndex ==
                segments.Length)
                return;

            var lastSegmentToBeConcealedIndex = minimalTouchedSegmentIndex - 1;

            var touchedSegmentsIndex = pathIndex switch
            {
                0 => _touchedLSegmentIndex,
                1 => _touchedMSegmentIndex,
                2 => _touchedRSegmentIndex,
                _ => -1
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

            var segmentToBeConcealedIndex = math.min(_touchedRSegmentIndex,
                math.min(_touchedLSegmentIndex, _touchedMSegmentIndex)) - 1;

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

        private void CheckAndJudge(TouchEventArgs e)
        {
            if (Slided)
                return;

            if (_touchedLSegmentIndex + _touchedMSegmentIndex + _touchedRSegmentIndex != 11)
                return;

            if ((segments[^1].sensorsL.Contains(e.SensorId) && _touchedLSegmentIndex == 3) ||
                (segments[^1].sensorsM.Contains(e.SensorId) && _touchedMSegmentIndex == 3) ||
                (segments[^1].sensorsR.Contains(e.SensorId) && _touchedRSegmentIndex == 3))
            {
                ConcealSegment(TernaryMinimal(_touchedLSegmentIndex, _touchedMSegmentIndex, _touchedRSegmentIndex) - 1,
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