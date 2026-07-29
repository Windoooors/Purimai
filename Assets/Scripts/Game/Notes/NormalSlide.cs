using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Notes
{
    public class NormalSlide : SlideBasedNote
    {
        [HideInInspector] public List<IndividualSlideBase> individualSlides = new();

        private IndividualStarMovementController _individualIndividualStarMovementController;

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

        protected override int GenerateSlideArrowObjects()
        {
            var count = 0;

            foreach (var individualSlideBase in individualSlides)
                count += individualSlideBase.GenerateSlideArrows(Order + count);

            return count;
        }

        protected override void InitializeVectorGraphicsUtility()
        {
            foreach (var individualSlideBase in individualSlides) individualSlideBase.InitializeVectorGraphicsUtility();
        }
        
        protected override (int judgeTiming, int starInLastSegmentDuration, Segment[] segments) InitializeSlideSegments()
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