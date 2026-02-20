using System;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using Game.Notes.SlideBasedNotes;

namespace Game.Notes
{
    public class NormalSlide : SlideBasedNote
    {
        public NormalSegment[] segments;

        private int _lastSegmentTouchedOnLeaveIndex = -1;
        private int _lastTouchedSegmentIndex = -1;

        private bool _slideStarted;

        private HashSet<string> _tappedSegmentSensorIds = new();

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
            JudgeSegment(e.SensorId, true);
        }

        protected override void OnSensorLeave(TouchEventArgs e)
        {
            JudgeSegment(e.SensorId, false);
        }

        private void JudgeSegment(string sensorId, bool isFromHold)
        {
            if (ChartPlayer.Instance.GetTime(true) < timing || !SlideContentRoot.activeSelf)
                return;

            for (var i = _lastTouchedSegmentIndex + 1; i < segments.Length; i++)
            {
                var segment = segments[i];

                if (!SensorContained(segment, sensorId) || !(i is 0 or 1 || segments[i - 2].touched))
                    continue;

                if (!_slideStarted)
                {
                    PlaySlideSound();
                    _slideStarted = true;
                }

                var jumpAllowed = IsJumpedTouchingSequenceAllowed();

                if (isFromHold)
                {
                    if (i - 1 >= 0 && (jumpAllowed || segments[i - 1].touched))
                    {
                        segments[i - 1].touched = true;

                        ConcealSegment(i - 1, false);
                        _lastTouchedSegmentIndex = i - 1;

                        if (i == segments.Length - 1)
                            Judge();

                        ConcealMiddleSegment(i);

                        break;
                    }
                }
                else
                {
                    if (i == segments.Length - 1)
                        break;

                    var touchingSequenceJumped = false;
                    if (i != _lastSegmentTouchedOnLeaveIndex)
                    {
                        touchingSequenceJumped = i - _lastSegmentTouchedOnLeaveIndex == 2;

                        _lastSegmentTouchedOnLeaveIndex = i;
                    }

                    if (!jumpAllowed && touchingSequenceJumped)
                        break;

                    if (i != 0 && !segments[i - 1].touched)
                        break;

                    segments[i].touched = true;
                    ConcealSegment(i, touchingSequenceJumped);

                    _lastTouchedSegmentIndex = i;
                }
            }
        }

        private bool SensorContained(NormalSegment segment, string sensorId)
        {
            return segment.mainSensor == sensorId ||
                   segment.sensorsNearby.Contains(sensorId);
        }

        private bool IsJumpedTouchingSequenceAllowed()
        {
            var interval = 0;

            if (slideType is NoteDataObject.SlideDataObject.SlideType.Line)
                interval = GetShortestInterval(fromLaneIndex + 1, toLaneIndexes[0] + 1);
            else if (slideType is NoteDataObject.SlideDataObject.SlideType.RotateLeft
                     or NoteDataObject.SlideDataObject.SlideType.RotateRight
                     or NoteDataObject.SlideDataObject.SlideType.RotateMinorArc)
                interval = CycleSlide.GetCycleInterval(fromLaneIndex + 1, toLaneIndexes[0] + 1, slideType);
            else if (slideType is NoteDataObject.SlideDataObject.SlideType.BigV)
                interval = 1;

            if (interval is 2 or 1)
                return false;

            return true;
        }
    }

    [Serializable]
    public class NormalSegment : Segment
    {
        public string[] sensorsNearby;
    }
}