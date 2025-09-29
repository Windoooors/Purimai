using System;
using System.Linq;
using ChartManagement;
using Notes.Slides;
using UnityEngine;

namespace Notes
{
    public class NormalSlide : SlideBasedNote
    {
        public NormalSegment[] segments; 
        private int _touchedSegmentIndex;
        private int _tappedSegmentIndex;

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

        protected override void OnSensorTap(TouchEventArgs e)
        {
            if (Slided)
                return;
            
            var segmentState = GetSegmentState(e.SensorId, _tappedSegmentIndex);

            if (!segmentState.activated)
                return;

            if (!IsJumpSensorAllowed() && segmentState.sensorJumped)
                return;
            
            _tappedSegmentIndex += segmentState.sensorJumped ? 2 : 1;

            if (_tappedSegmentIndex == segments.Length && !Slided)
            {
                ConcealSegment(_tappedSegmentIndex - 2, false);
                Judge();
            }

            if (_tappedSegmentIndex - 1 == _touchedSegmentIndex && _tappedSegmentIndex != segments.Length)
                ConcealMiddleSegment(_tappedSegmentIndex - 1);
        }

        private bool SensorContained(int segmentIndex, string sensorId)
        {
            return segments[segmentIndex].mainSensor == sensorId ||
                   segments[segmentIndex].sensorsNearby.Contains(sensorId);
        }

        protected override void OnSensorHold(TouchEventArgs e,
            bool sensorJumpedForLastSegment = false)
        {
            if (Slided)
                return;
            
            var segmentState = GetSegmentState(e.SensorId, _touchedSegmentIndex);
            var sensorJumped = segmentState.sensorJumped;

            if (!segmentState.activated)
                return;

            if (!IsJumpSensorAllowed() && sensorJumped)
                return;

            if (_touchedSegmentIndex == segments.Length - 1)
            {
                if (!Slided)
                {
                    ConcealSegment(_touchedSegmentIndex - 1, sensorJumpedForLastSegment);
                    Judge();
                }

                return;
            }

            ConcealSegment(_touchedSegmentIndex, sensorJumpedForLastSegment);
            
            _touchedSegmentIndex++;

            if (_tappedSegmentIndex < _touchedSegmentIndex)
                _tappedSegmentIndex = _touchedSegmentIndex;

            if (sensorJumped)
                OnSensorHold(e, true);
        }

        private bool IsJumpSensorAllowed()
        {
            var interval = 0;

            if (slideType is NoteDataObject.SlideDataObject.SlideType.Line)
                interval = GetShortestInterval(fromLaneIndex + 1, toLaneIndexes[0] + 1);
            else if (slideType is NoteDataObject.SlideDataObject.SlideType.RotateLeft
                     or NoteDataObject.SlideDataObject.SlideType.RotateRight
                     or NoteDataObject.SlideDataObject.SlideType.RotateMinorArc)
                interval = CycleSlide.GetCycleInterval(fromLaneIndex + 1, toLaneIndexes[0] + 1, slideType);

            if (interval is 2 or 1)
                return false;

            return true;
        }

        private (bool sensorJumped, bool activated) GetSegmentState(string sensorId, int index)
        {
            if (timing > ChartPlayer.Instance.time)
                return (false, false);

            if (index == segments.Length)
                return (false, false);

            var sensorJumped =
                index + 1 != segments.Length &&
                SensorContained(index + 1, sensorId);

            var activated =
                (SensorContained(index, sensorId) || sensorJumped) &&
                index < segments.Length;

            return (sensorJumped, activated);
        }
    }

    [Serializable]
    public class NormalSegment : Segment
    {
        public string[] sensorsNearby;
    }
}