using System;
using System.Collections.Generic;
using Game.ChartManagement;
using UI.Result;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Notes
{
    public abstract class IndividualSlideBase : MonoBehaviour
    {
        public string svgAssetPath;

        public int slideArrowCount;
        public SlideBasedNote.Segment[] segments;
        protected bool IsClockwise;
        public NoteDataObject.IndividualSlideDataObject individualSlideDataObject;
        public bool flipPathY = false;
        public float pathRotation = 0;
        protected int[] SlideJudgeDisplaySpriteIndexes;

        private VectorGraphicsUtility _graphicsUtility;

        public int GenerateSlideArrows()
        {
            var prefab = NoteGenerator.Instance.slideArrowPrefab;
            
            for (int i = 0; i < slideArrowCount; i++)
            {
                var arrowInstance = Instantiate(prefab, transform);

                var pair = _graphicsUtility.GetPositionRotationPair(i / (float)slideArrowCount, true);
                arrowInstance.transform.position = pair.position;
                arrowInstance.transform.rotation = pair.rotation;
            }
            
            return slideArrowCount;
        }

        public void InitializeVectorGraphicsUtility()
        {
            _graphicsUtility = new VectorGraphicsUtility(svgAssetPath, pathRotation, flipPathY,
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

        public abstract void InitializeSlideDirection();
    }
}