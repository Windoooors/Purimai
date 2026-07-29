using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Notes
{
    public abstract class IndividualSlideBase : MonoBehaviour
    {
        public IndividualStarMovementController star;
        public string svgAssetPath;
        public NormalSlide parentNormalSlide;

        public int slideArrowCount;
        public SlideBasedNote.Segment[] segments;
        public bool flipPathY;
        public float pathRotation;
        
        public SpriteRenderer judgeDisplaySpriteRenderer;
        
        public VectorGraphicsUtility GraphicsUtility { get; private set; }

        private float? _pathLength;

        private readonly List<SpriteRenderer> _slideArrowSpriteRenderers = new();
        public NoteDataObject.IndividualSlideDataObject individualSlideDataObject;
        protected bool IsClockwise;
        protected int[] SlideJudgeDisplaySpriteIndexes;
        
        public virtual void UpdateJudgeDisplayDirection(int judgeSpriteGroupIndex)
        {
            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is > 265 and <= 365 or > -5 and <= 95;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance
                .slideJudgeDisplaySprites[judgeSpriteGroupIndex]
                .normalSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? SlideJudgeDisplaySpriteIndexes[1]
                        : SlideJudgeDisplaySpriteIndexes[0]];

            if (!judgeSpriteNeedsChange)
                judgeDisplaySpriteRenderer.transform.eulerAngles += new Vector3(0, 0, 180);
        }

        public int GenerateSlideArrows(int parentalOrder)
        {
            var prefab = NoteGenerator.Instance.slideArrowPrefab;
            var order = 0;

            for (var i = 0; i < slideArrowCount; i++)
            {
                var division = slideArrowCount + 1.35;
                var currentProgress = (float)i + 1;

                if (individualSlideDataObject.Type is NoteDataObject.SlideType.Line)
                {
                    division -= 0.45f;
                    currentProgress -= 0.65f;
                }

                if ((int)individualSlideDataObject.Type is 0 or 1 or 2)
                {
                    division -= 1.33f;
                    currentProgress -= 0.60f;
                }

                var progress = (float)(currentProgress / division);

                var arrowInstance = Instantiate(prefab, transform);

                var pair = GraphicsUtility.GetPositionRotationPair(progress, true);
                arrowInstance.transform.position = pair.position;
                arrowInstance.transform.rotation = pair.rotation;
                var spriteRenderer = arrowInstance.GetComponent<SpriteRenderer>();

                spriteRenderer.sortingOrder = order++ + parentalOrder;
                if (parentNormalSlide.IsEach)
                    spriteRenderer.sprite = NoteGenerator.Instance.slideEachSprite;

                arrowInstance.transform.SetParent(transform);

                _slideArrowSpriteRenderers.Add(spriteRenderer);
            }

            if (parentNormalSlide.IsEach)
                star.spriteRenderer.sprite = NoteGenerator.Instance.eachStarSprite;

            return slideArrowCount;
        }

        public void InitializeVectorGraphicsUtility()
        {
            GraphicsUtility = new VectorGraphicsUtility(svgAssetPath, pathRotation, flipPathY,
                Lanes.Instance.endPoints[individualSlideDataObject.From - 1].position, 180);
        }

        public static int GetShortestInterval(int fromLane, int toLane)
        {
            if (fromLane == toLane) return 0;

            var clockwiseInterval = (toLane - fromLane + 8) % 8;
            var counterClockwiseInterval = (fromLane - toLane + 8) % 8;

            return math.min(clockwiseInterval, counterClockwiseInterval);
        }

        public static (int clockwiseInterval, int counterClockwiseInterval) GetIntervalInBothWays(int start, int end)
        {
            var clockwise = (end - start + 8) % 8;

            var counterClockwise = (start - end + 8) % 8;

            return (clockwise, counterClockwise);
        }

        protected void MirrorSlideSensorIds()
        {
            foreach (var segment in segments)
                segment.sensors.ToList().ForEach(x => { x.sensor = SlideBasedNote.GetMirroredSensorId(x.sensor); });
        }

        public abstract void InitializeSlideDirection();

        public (int judgeTiming, int starInLastSegmentDuration) InitializeSlideSegments(int individualSlideDuration,
            int individualStartTiming)
        {
            var previousMatchedArrowIndex = -1;

            foreach (var segment in segments)
            {
                if (segment.slideSpriteRenderersOutsideSensorArea.Length > 0 ||
                    segment.slideSpriteRenderersWithinSensorArea.Length > 0)
                {
                    var slideSpriteRendererList = new List<SpriteRenderer>();
                    slideSpriteRendererList.AddRange(segment.slideSpriteRenderersOutsideSensorArea);
                    slideSpriteRendererList.AddRange(segment.slideSpriteRenderersWithinSensorArea);
                    segment.slideSpriteRenderers = slideSpriteRendererList.ToArray();
                    continue;
                }

                var mainSensor = segment.sensors.FirstOrDefault(x => x.type == SlideBasedNote.Segment.SensorType.Main)
                    ?.sensor;

                var matchedSensorShape = SensorShape.SensorShapes.Find(x => x.sensorId == mainSensor);

                var spriteWithinAreaList = new List<SpriteRenderer>();
                var spriteOutsideAreaList = new List<SpriteRenderer>();

                var startIndex = previousMatchedArrowIndex + 1;

                var count = _slideArrowSpriteRenderers.Count;
                var sensorCollider = matchedSensorShape.GetComponent<Collider2D>();
                var slideType = individualSlideDataObject.Type;

                bool IsOverlapping(int index)
                {
                    if (index < 0 || index >= count) return false;
                    return SlideBasedNote.ArrowOverlapsOnSensor(_slideArrowSpriteRenderers[index], slideType,
                        sensorCollider);
                }

                for (var i = startIndex; i < count; i++)
                {
                    var prevOverlaps = IsOverlapping(i - 1);
                    var currentOverlaps = IsOverlapping(i);

                    if (i > startIndex && (!prevOverlaps || !currentOverlaps)) break;

                    if (IsOverlapping(i))
                        spriteWithinAreaList.Add(_slideArrowSpriteRenderers[i]);
                    else
                        spriteOutsideAreaList.Add(_slideArrowSpriteRenderers[i]);

                    previousMatchedArrowIndex = i;
                }

                segment.slideSpriteRenderersWithinSensorArea = spriteWithinAreaList.ToArray();
                segment.slideSpriteRenderersOutsideSensorArea = spriteOutsideAreaList.ToArray();

                var slideSpriteRenderersList = new List<SpriteRenderer>();

                slideSpriteRenderersList.AddRange(segment.slideSpriteRenderersWithinSensorArea);
                slideSpriteRenderersList.AddRange(segment.slideSpriteRenderersOutsideSensorArea);

                segment.slideSpriteRenderers = slideSpriteRenderersList.ToArray();
            }

            var lastSegmentArrowCount = segments[^1].slideSpriteRenderersWithinSensorArea.Length;
            var slideJudgeTiming = (int)((float)(_slideArrowSpriteRenderers.Count - lastSegmentArrowCount) /
                                         _slideArrowSpriteRenderers.Count * individualSlideDuration
                                         + individualStartTiming);

            var starInLastSegmentDuration =
                (int)((float)lastSegmentArrowCount / _slideArrowSpriteRenderers.Count * individualSlideDuration);

            return (slideJudgeTiming, starInLastSegmentDuration);
        }

        public float GetSlidePathLength()
        {
            if (GraphicsUtility == null)
                return -1;

            return _pathLength ??= GraphicsUtility.GetTotalLength();
        }

        public void InitializeSensorIds()
        {
            var chainedSlide = parentNormalSlide.individualSlides.Count > 1;

            foreach (var segment in segments)
            {
                foreach (var segmentSensor in segment.sensors)
                    segmentSensor.sensor =
                        SlideBasedNote.GetUpdatedSensorId(segmentSensor.sensor,
                            individualSlideDataObject.From - 1);

                if (chainedSlide)
                    segment.canBeSkipped = true;
            }
        }
    }
}