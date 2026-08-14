using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Notes
{
    public class NormalSlide : SlideBasedNote
    {
        [HideInInspector] public List<IndividualSlideBase> individualSlides = new();

        private IndividualStarMovementController _individualIndividualStarMovementController;

        private int _lastSegmentTouchedOnLeaveIndex = -1;

        private int _lastTouchedSegmentIndex = -1;
        private bool _slideStarted;

        private readonly List<SpriteRenderer> _slideArrowSpriteRenderers = new();

        protected override List<SpriteRenderer> GetArrowSpriteRenderers() => _slideArrowSpriteRenderers;

        protected override IStarMovementController[] GetStars()
        {
            var stars = individualSlides.Select(x => x.star).ToArray();

            var chainedStarMovementController = new ChainedStarMovementController(stars);

            return new IStarMovementController[] { chainedStarMovementController };
        }

        protected override SpriteRenderer GetJudgeDisplaySpriteRenderer()
        {
            return individualSlides[^1].judgeDisplaySpriteRenderer;
        }

        protected override void UpdateJudgeDisplayDirection(int displaySpriteIndex)
        {
            individualSlides[^1].UpdateJudgeDisplayDirection(displaySpriteIndex);
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
            if (ChartPlayer.Instance.TimeInMilliseconds + 50 < Timing || !SlideContentRoot.activeSelf)
                return;

            for (var i = _lastTouchedSegmentIndex + 1; i < Segments.Length; i++)
            {
                var segment = Segments[i];

                if (!SensorContained(segment, sensorId) || !(i is 0 or 1 || Segments[i - 2].touched))
                    continue;

                if (!_slideStarted)
                {
                    PlaySlideSound();
                    _slideStarted = true;
                }

                if (isFromHold)
                {
                    if (i - 1 >= 0 && (Segments[i - 1].canBeSkipped || Segments[i - 1].tapped))
                    {
                        Segments[i - 1].touched = true;
                        Segments[i].tapped = true;

                        ConcealSegment(i - 1, false);
                        _lastTouchedSegmentIndex = i - 1;

                        if (i == Segments.Length - 1)
                            Judge();

                        ConcealMiddleSegment(i);

                        break;
                    }
                }
                else
                {
                    if (i == Segments.Length - 1)
                        break;

                    var touchingSequenceJumped = false;
                    if (i != _lastSegmentTouchedOnLeaveIndex)
                    {
                        touchingSequenceJumped = i - _lastSegmentTouchedOnLeaveIndex == 2;

                        _lastSegmentTouchedOnLeaveIndex = i;
                    }

                    if (i != 0 &&
                        (!Segments[i - 1].touched || (!Segments[i - 1].canBeSkipped && touchingSequenceJumped)))
                        break;

                    Segments[i].touched = true;
                    ConcealSegment(i, touchingSequenceJumped);

                    _lastTouchedSegmentIndex = i;
                }
            }
        }

        private bool SensorContained(Segment segment, string sensorId)
        {
            return segment.sensors.Any(x => x.sensor == sensorId);
        }

        protected override int GenerateSlideArrowObjects()
        {
            transform.position = Vector3.zero;

            var count = 0;

            var arrowCount = individualSlides.Sum(x => x.slideArrowCount);

            foreach (var individualSlideBase in individualSlides)
                count += individualSlideBase.GenerateSlideArrows(Order - count + arrowCount);

            _slideArrowSpriteRenderers.AddRange(individualSlides.SelectMany(x => x.SlideArrowSpriteRenderers));

            return count;
        }

        protected override void InitializeVectorGraphicsUtility()
        {
            foreach (var individualSlideBase in individualSlides) individualSlideBase.InitializeVectorGraphicsUtility();
        }

        protected override (int judgeTiming, int starInLastSegmentDuration, Segment[] segments)
            InitializeSlideSegments()
        {
            float startTimingF = WaitDuration + Timing;
            var judgeTiming = 0;
            var starInLastSegmentDuration = 0;

            var totalLength = individualSlides.Sum(x => x.GetSlidePathLength());

            var slideSegments = new List<Segment>();

            foreach (var individualSlideBase in individualSlides)
            {
                var slidePathLength = individualSlideBase.GetSlidePathLength();
                var individualSlideDurationF = SlideDuration * (slidePathLength / totalLength);
                var dataPair = individualSlideBase.InitializeSlideSegments((int)individualSlideDurationF
                    , (int)startTimingF);

                startTimingF += individualSlideDurationF;

                judgeTiming = dataPair.judgeTiming;
                starInLastSegmentDuration = dataPair.starInLastSegmentDuration;

                if (slideSegments.Count > 0)
                {
                    var temp = slideSegments[^1].slideSpriteRenderersWithinSensorArea.ToList();
                    temp.AddRange(slideSegments[^1].slideSpriteRenderersOutsideSensorArea);

                    slideSegments[^1].slideSpriteRenderersOutsideSensorArea = temp.ToArray();
                    slideSegments[^1].slideSpriteRenderersWithinSensorArea =
                        individualSlideBase.segments[0].slideSpriteRenderersWithinSensorArea;

                    temp.AddRange(slideSegments[^1].slideSpriteRenderersWithinSensorArea);

                    slideSegments[^1].slideSpriteRenderers = temp.ToArray();

                    slideSegments.AddRange(
                        individualSlideBase.segments.Where(x => x != individualSlideBase.segments[0]));

                    continue;
                }

                slideSegments.AddRange(individualSlideBase.segments);
            }

            return (judgeTiming, starInLastSegmentDuration, slideSegments.ToArray());
        }

        public override void AddAutoPlayKeyFrame()
        {
            foreach (var segment in Segments)
            {
                var index = Segments.ToList().IndexOf(segment);

                if (index == 0)
                    continue;

                float tapTime;
                float leaveTime;

                if (index == Segments.Length - 1)
                {
                    tapTime = JudgeTiming;
                    leaveTime = JudgeTiming;
                }
                else
                {
                    tapTime = index / (float)Segments.Length * SlideDuration + Timing + WaitDuration;
                    leaveTime = (index + 1) / (float)Segments.Length * SlideDuration + Timing + WaitDuration;
                }

                var mainSensor = segment.sensors.FirstOrDefault(x => x.type == Segment.SensorType.Main)?.sensor;

                var list = AutoPlayer.KeyFrameManager.GetKeyFrames(mainSensor);

                list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)tapTime));
                list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, (int)tapTime));
                list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressUp, (int)leaveTime));
                list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)leaveTime));
            }
        }

        protected override void InitializePathRotation()
        {
            foreach (var individualSlideBase in individualSlides) individualSlideBase.InitializeSlideDirection();
        }

        protected override void InitializeSlideSensorIds()
        {
            foreach (var individualSlideBase in individualSlides) individualSlideBase.InitializeSensorIds();
        }
    }
}