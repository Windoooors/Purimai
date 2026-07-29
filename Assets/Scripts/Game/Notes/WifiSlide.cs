using System;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Notes
{
    public class WifiSlide : SlideBasedNote
    {
        private const int WifiSlideArrowCount = 11;
        [FormerlySerializedAs("stars")] public WifiStarMovementController[] wifiStars;
        public Segment[] wifiSegments;
        
        public SpriteRenderer judgeDisplaySpriteRenderer;

        protected override SpriteRenderer GetJudgeDisplaySpriteRenderer()
        {
            return judgeDisplaySpriteRenderer;
        }

        public string svgAssetPath;
        public float pathRotation;
        private readonly List<SpriteRenderer> _slideArrowSpriteRenderers = new();

        private readonly int[][] _slideArrowGroups =
        {
            new[] { 0, 1 }, new[] { 2, 3, 4 }, new[] { 5, 6, 7 }, new[] { 8, 9, 10 }
        };

        private IndividualStarMovementController _individualIndividualStarMovementController;

        private VectorGraphicsUtility _vectorGraphicsUtility;

        public NoteDataObject.IndividualSlideDataObject slideData;

        protected override (int judgeTiming, int starInLastSegmentDuration, Segment[] segments) InitializeSlideSegments()
        {
            var lastSegmentDuration = _slideArrowGroups[^1].Length / WifiSlideArrowCount * SlideDuration;

            for (var i = 0; i < wifiSegments.Length; i++)
            {
                var wifiSegment = wifiSegments[i];

                wifiSegment.slideSpriteRenderers =
                    _slideArrowGroups[i].Select(x => _slideArrowSpriteRenderers[x]).ToArray();
                wifiSegment.slideSpriteRenderersWithinSensorArea = wifiSegment.slideSpriteRenderers;
                wifiSegment.slideSpriteRenderersOutsideSensorArea = Array.Empty<SpriteRenderer>();
            }
            
            return (Timing + WaitDuration + SlideDuration - lastSegmentDuration, lastSegmentDuration, wifiSegments);
        }

        protected override IStarMovementController[] GetStars()
        {
            return wifiStars;
        }

        private void GenerateSlideArrows()
        {
            for (var i = 0; i < WifiSlideArrowCount; i++)
            {
                var division = WifiSlideArrowCount + 1.35;

                var currentProgress = (float)i + 1;

                var progress = (float)(currentProgress / division + (currentProgress - 2) / 30
                                       - (currentProgress - 1) * 0.48f / division);

                var prefab = NoteGenerator.Instance.slideArrowPrefab;

                var arrowInstance = Instantiate(prefab, transform);

                var pair = _vectorGraphicsUtility.GetPositionRotationPair(progress, true);
                arrowInstance.transform.position = pair.position;
                arrowInstance.transform.rotation = pair.rotation;

                var slideSpriteRenderer = arrowInstance.GetComponent<SpriteRenderer>();

                slideSpriteRenderer.sprite = IsEach
                    ? NoteGenerator.Instance.wifiSlideEachSprites[i]
                    : NoteGenerator.Instance.wifiSlideSprites[i];

                slideSpriteRenderer.sortingOrder = i + Order;

                arrowInstance.transform.eulerAngles = new Vector3(0, 0, 315) +
                                                      arrowInstance.transform
                                                          .parent.eulerAngles;

                _slideArrowSpriteRenderers.Add(slideSpriteRenderer);
            }

            if (IsEach)
                foreach (var starMovementController in wifiStars)
                    starMovementController.spriteRenderer.sprite = NoteGenerator.Instance.eachStarSprite;
        }
        
        protected override void UpdateJudgeDisplayDirection(int judgeDisplaySpriteGroupIndex)
        {
            var judgeSpriteNeedsChange =
                JudgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is >= 265 and <= 365 or >= -5 and <= 95;

            JudgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance
                .slideJudgeDisplaySprites[judgeDisplaySpriteGroupIndex]
                .wifiSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? 0
                        : 1];

            if (!judgeSpriteNeedsChange) JudgeDisplaySpriteRenderer.transform.eulerAngles += new Vector3(0, 0, 180);
        }

        protected override int GenerateSlideArrowObjects()
        {
            GenerateSlideArrows();
            return WifiSlideArrowCount;
        }

        protected override void InitializeVectorGraphicsUtility()
        {
            _vectorGraphicsUtility = new VectorGraphicsUtility(svgAssetPath, pathRotation, false,
                Lanes.Instance.endPoints[slideData.From - 1].position, 180);
        }

        protected override void InitializeSlideSensorIds()
        {
            foreach (var wifiSegment in wifiSegments)
            foreach (var wifiSegmentSensor in wifiSegment.sensors)
                wifiSegmentSensor.sensor =
                    GetUpdatedSensorId(wifiSegmentSensor.sensor, slideData.From - 1);
        }

        protected override void InitializePathRotation()
        {
            transform.Rotate(new Vector3(0, 0, -45f * (slideData.From - 1)));

            pathRotation = -45f * (slideData.From - 1);
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

                var lList = AutoPlayer.KeyFrameManager.GetKeyFrames(segment.sensors
                    .FirstOrDefault(x => x.lane == Segment.Lane.Left)?.sensor);

                lList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)tapTime));
                lList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, (int)tapTime));
                lList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressUp, (int)leaveTime));
                lList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)leaveTime));

                var mList = AutoPlayer.KeyFrameManager.GetKeyFrames(segment.sensors
                    .FirstOrDefault(x => x.lane == Segment.Lane.Center)?.sensor);

                mList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)tapTime));
                mList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, (int)tapTime));
                mList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressUp, (int)leaveTime));
                mList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)leaveTime));

                var rList = AutoPlayer.KeyFrameManager.GetKeyFrames(segment.sensors
                    .FirstOrDefault(x => x.lane == Segment.Lane.Right)?.sensor);

                rList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)tapTime));
                rList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, (int)tapTime));
                rList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressUp, (int)leaveTime));
                rList.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.Hold, (int)leaveTime));
            }
        }
    }
}