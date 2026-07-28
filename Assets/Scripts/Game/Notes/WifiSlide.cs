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
        [FormerlySerializedAs("stars")] public StarMovementController[] wifiStars;
        public Segment[] wifiSegments;
        private const int WifiSlideArrowCount = 11;
        private readonly List<SpriteRenderer> _slideArrowSpriteRenderers = new();
        
        private int[][] _slideArrowGroups = 
        {
            new[] { 0, 1 }, new[] { 2, 3 }, new[] { 4, 5, 6 }, new[] { 7, 8 }, new[] { 9, 10 }
        };
        
        public string svgAssetPath;
        public float pathRotation = 0;
        
        public NoteDataObject.IndividualSlideDataObject slideData;
        
        private VectorGraphicsUtility _vectorGraphicsUtility;
        
        private StarMovementController _starMovementController;

        protected override (int judgeTiming, int starInLastSegmentDuration) InitializeSlideSegments()
        {
            var lastSegmentDuration = _slideArrowGroups[^1].Length / WifiSlideArrowCount * SlideDuration;

            for (var i = 0;i < WifiSlideArrowCount;i++)
            {
                var wifiSegment = wifiSegments[i];

                wifiSegment.slideSpriteRenderers =
                    _slideArrowGroups[i].Select(x => _slideArrowSpriteRenderers[x]).ToArray();
                wifiSegment.slideSpriteRenderersWithinSensorArea = wifiSegment.slideSpriteRenderers;
                wifiSegment.slideSpriteRenderersOutsideSensorArea = Array.Empty<SpriteRenderer>();
            }
            
            segments = wifiSegments;
            
            return (Timing + WaitDuration + SlideDuration - lastSegmentDuration, lastSegmentDuration);
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
                {
                    starMovementController.spriteRenderer.sprite = NoteGenerator.Instance.eachStarSprite;
                }
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
            {
                foreach (var wifiSegmentSensor in wifiSegment.sensors)
                {
                    wifiSegmentSensor.sensor = 
                        GetUpdatedSensorId(wifiSegmentSensor.sensor, slideData.From - 1);
                }
            }
        }

        protected override void InitializePathRotation()
        {
            transform.Rotate(new Vector3(0, 0, -45f * (slideData.From - 1)));

            pathRotation = -45f * (slideData.From - 1);
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
                    tapTime = JudgeTiming;
                    leaveTime = JudgeTiming;
                }
                else
                {
                    tapTime = index / (float)segments.Length * SlideDuration + Timing + WaitDuration;
                    leaveTime = (index + 1) / (float)segments.Length * SlideDuration + Timing + WaitDuration;
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