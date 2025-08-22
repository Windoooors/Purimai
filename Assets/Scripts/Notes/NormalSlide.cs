using System;
using System.Linq;
using ChartManagement;
using Notes.Slides;

namespace Notes
{
    public class NormalSlide : SlideBasedNote
    {
        public NormalSegment[] segments;
        private int _touchedSegmentsIndex;

        protected override void UpdateUniversalSegments()
        {
            UniversalSegments.AddRange(segments);
        }

        protected void MirrorSlideSensorIds()
        {
            foreach (var segment in segments)
            {
                segment.mainSensor = GetMirroredSensorId(segment.mainSensor);

                for (var i = 0; i < segment.sensorsNearby.Length; i++)
                    segment.sensorsNearby[i] = GetMirroredSensorId(segment.sensorsNearby[i]);
            }
        }

        protected override void InitializeSlideSensorIds()
        {
            foreach (var segment in segments)
            {
                segment.mainSensor = GetUpdatedSensorId(segment.mainSensor);

                for (var i = 0; i < segment.sensorsNearby.Length; i++)
                    segment.sensorsNearby[i] = GetUpdatedSensorId(segment.sensorsNearby[i]);
            }
        }

        private bool SensorContained(int segmentIndex, string sensorId)
        {
            return segments[segmentIndex].mainSensor == sensorId ||
                   segments[segmentIndex].sensorsNearby.Contains(sensorId);
        }

        protected override void ProcessSlideTap(SimulatedSensor.TouchEventArgs e,
            bool sensorJumpedForLastSegment = false)
        {
            var segmentState = GetSegmentState(e.SensorId);
            var sensorJumped = segmentState.sensorJumped;

            if (!segmentState.activated)
                return;

            if (sensorJumped)
                return;

            ConcealMiddleSegment(_touchedSegmentsIndex);
        }

        protected override void ProcessSlideHold(SimulatedSensor.TouchEventArgs e,
            bool sensorJumpedForLastSegment = false)
        {
            if (_touchedSegmentsIndex == segments.Length - 1)
            {
                ConcealMiddleSegment(_touchedSegmentsIndex);
                return;
            }

            var segmentState = GetSegmentState(e.SensorId);
            var sensorJumped = segmentState.sensorJumped;

            if (!segmentState.activated)
                return;

            var interval = 0;

            if (slideType is NoteDataObject.SlideDataObject.SlideType.Line)
                interval = GetShortestInterval(fromLaneIndex + 1, toLaneIndexes[0] + 1);
            else if (slideType is NoteDataObject.SlideDataObject.SlideType.RotateLeft
                     or NoteDataObject.SlideDataObject.SlideType.RotateRight
                     or NoteDataObject.SlideDataObject.SlideType.RotateMinorArc)
                interval = CycleSlide.GetCycleInterval(fromLaneIndex + 1, toLaneIndexes[0] + 1, slideType);

            if (sensorJumped &&
                interval == 2)
                return;

            ConcealSegment(_touchedSegmentsIndex, sensorJumpedForLastSegment);

            _touchedSegmentsIndex++;
            if (sensorJumped)
                ProcessSlideHold(e, true);
        }

        private (bool sensorJumped, bool activated) GetSegmentState(string sensorId)
        {
            if (timing - 100 > ChartPlayer.Instance.time)
                return (false, false);

            if (_touchedSegmentsIndex == segments.Length)
                return (false, false);

            var sensorJumped =
                _touchedSegmentsIndex + 1 != segments.Length &&
                SensorContained(_touchedSegmentsIndex + 1, sensorId);

            var activated =
                (SensorContained(_touchedSegmentsIndex, sensorId) || sensorJumped) &&
                _touchedSegmentsIndex < segments.Length;

            return (sensorJumped, activated);
        }
    }

    [Serializable]
    public class NormalSegment : Segment
    {
        public string[] sensorsNearby;
    }
}