using System;
using System.Collections.Generic;
using System.Linq;

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

        public override void AddAutoPlayKeyFrame()
        {
            foreach (var segment in segments)
            {
                var index = segments.ToList().IndexOf(segment);

                if (index == 0)
                    continue;

                float tapTime;
                float leaveTime;

                if (index == segments.Length - 1)
                {
                    tapTime = SlideJudgeTiming;
                    leaveTime = SlideJudgeTiming;
                }
                else
                {
                    tapTime = index / (float)segments.Length * slideDuration + timing + waitDuration;
                    leaveTime = (index + 1) / (float)segments.Length * slideDuration + timing + waitDuration;
                }

                var list = AutoPlayer.KeyFrameManager.GetKeyFrames(segment.mainSensor);

                list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)tapTime));
                list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, (int)tapTime));
                list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressUp, (int)leaveTime));
                list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)leaveTime));
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
            if (ChartPlayer.Instance.TimeInMilliseconds + 50 < timing || SlideContentRoot.layer != ShownLayer)
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

                if (isFromHold)
                {
                    if (i - 1 >= 0 && (segments[i - 1].canBeSkipped || segments[i - 1].tapped))
                    {
                        segments[i - 1].touched = true;
                        segments[i].tapped = true;

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

                    if (i != 0 &&
                        (!segments[i - 1].touched || (!segments[i - 1].canBeSkipped && touchingSequenceJumped)))
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
    }

    [Serializable]
    public class NormalSegment : Segment
    {
        public string[] sensorsNearby;
    }
}