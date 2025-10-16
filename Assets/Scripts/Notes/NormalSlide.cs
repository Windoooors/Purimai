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

        private bool _sensorJumped;
        private int _touchedSegmentIndex;

        private string _lastHeldSensorId = "";

        private bool _isLastSegmentTouchedByHolding;

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

        protected override void OnSensorHold(TouchEventArgs e)
        {
            if (Slided)
                return;

            var segmentState = GetSegmentState(e.SensorId, _touchedSegmentIndex);

            if (!segmentState.activated)
                return;

            if (!IsJumpSensorAllowed() && segmentState.sensorJumped)
                return;

            if (segmentState.sensorJumped)
            {
                _sensorJumped = true;
                _touchedSegmentIndex++;
                ConcealSegment(_touchedSegmentIndex - 1, false);
            }
            
            if (_lastHeldSensorId != e.SensorId)
            {
                if (_isLastSegmentTouchedByHolding && _sensorJumped)
                    _sensorJumped = false;
                
                _isLastSegmentTouchedByHolding = true;
            }
            
            _lastHeldSensorId = e.SensorId;

            if (_touchedSegmentIndex == segments.Length - 1 && !Slided)
            {
                ConcealSegment(_touchedSegmentIndex - 1, false);
                Judge();
            }

            if (_touchedSegmentIndex != segments.Length - 1)
                ConcealMiddleSegment(_touchedSegmentIndex);
        }

        private bool SensorContained(int segmentIndex, string sensorId)
        {
            return segments[segmentIndex].mainSensor == sensorId ||
                   segments[segmentIndex].sensorsNearby.Contains(sensorId);
        }

        protected override void OnSensorLeave(TouchEventArgs e)
        {
            if (Slided)
                return;

            var segmentState = GetSegmentState(e.SensorId, _touchedSegmentIndex);
            var sensorJumped = segmentState.sensorJumped;

            if (!segmentState.activated)
                return;

            if (sensorJumped)
                return;

            ConcealSegment(_touchedSegmentIndex, _sensorJumped);

            _sensorJumped = false;
            
            _isLastSegmentTouchedByHolding = false;

            _touchedSegmentIndex++;
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