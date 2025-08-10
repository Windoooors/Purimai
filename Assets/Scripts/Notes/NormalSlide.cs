using System;
using System.Linq;
using ChartManagement;
using LitMotion;
using Notes.Slides;
using UnityEngine;

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

        protected override void ProcessSlideTouch(SimulatedSensor.TouchEventArgs e,
            bool sensorJumpedForLastSegment = false)
        {
            if (timing - 100 > ChartPlayer.Instance.time)
                return;

            if (_touchedSegmentsIndex == segments.Length - 1 || _touchedSegmentsIndex == segments.Length)
                return;

            var sensorJumped =
                _touchedSegmentsIndex + 1 != segments.Length &&
                SensorContained(_touchedSegmentsIndex + 1, e.SensorId);

            var activated =
                (SensorContained(_touchedSegmentsIndex, e.SensorId) || sensorJumped) &&
                _touchedSegmentsIndex < segments.Length;

            if (!activated)
                return;

            if (_touchedSegmentsIndex - 1 != -1)
                segments[_touchedSegmentsIndex - 1].slideSpriteRenderers[^1].color = new Color(0, 0, 0, 0);

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

            var segment = segments[_touchedSegmentsIndex];

            foreach (var motionHandle in segment.MotionHandles) motionHandle.TryCancel();

            foreach (var slideSprite in segment.slideSpriteRenderers) slideSprite.color = new Color(0, 0, 0, 0);

            if (_touchedSegmentsIndex != segments.Length - 2 && sensorJumpedForLastSegment)
                segment.slideSpriteRenderers[^1].color = new Color(1, 1, 1, 0.5f);

            _touchedSegmentsIndex++;
            if (sensorJumped)
                ProcessSlideTouch(e, true);
        }
    }

    [Serializable]
    public class NormalSegment : Segment
    {
        public string[] sensorsNearby;
    }
}