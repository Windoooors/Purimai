using System;
using System.Collections.Generic;
using Game.ChartManagement;
using LitMotion;
using UnityEngine;

namespace Game.Notes
{
    public abstract class SlideBasedNote : NoteBase
    {
        public Segment[] Segments{ get; private set; }
        public IStarMovementController[] Stars{ get; private set; }
        public NoteDataObject.SlideDataObject SlideDataObject { get; private set; }
        public SpriteRenderer JudgeDisplaySpriteRenderer { get; private set; }
        
        protected int Order { get; private set; }
        public int Timing { get; private set; }
        public bool IsEach { get; private set; }
        public bool SuddenlyAppears { get; private set; }
        protected int WaitDuration { get; private set; }
        public int SlideDuration { get; private set; }

        public int JudgeTiming { get; private set; }
        public int SlideInLastSegmentDuration { get; private set; }

        public void Initialize(NoteDataObject.SlideDataObject slideDataObject, bool isSlideEach, int noteTiming,
            ref int slideArrowOrder)
        {
            Order = -slideArrowOrder;
            Timing = noteTiming;
            IsEach = isSlideEach;
            WaitDuration = slideDataObject.WaitDuration;
            SlideDuration = slideDataObject.SlideDuration;

            SuddenlyAppears = slideDataObject.SuddenlyAppears;
            transform.position = Vector3.zero;

            InitializePathRotation();
            InitializeVectorGraphicsUtility();
            InitializeSlideSensorIds();

            slideArrowOrder -= GenerateSlideArrowObjects();

            var dataPair = InitializeSlideSegments();
            JudgeTiming = dataPair.judgeTiming;
            SlideInLastSegmentDuration = dataPair.starInLastSegmentDuration;
            Segments = dataPair.segments;
            
            SlideDataObject = slideDataObject;
            Stars = GetStars();
            
            JudgeDisplaySpriteRenderer =  GetJudgeDisplaySpriteRenderer();
            
            foreach (var starMovementController in Stars)
            {
                starMovementController.Initialize();
            }

            UpdateJudgeDisplayDirection(0);
        }

        protected abstract int GenerateSlideArrowObjects();
        protected abstract void InitializeVectorGraphicsUtility();
        protected abstract void InitializePathRotation();
        protected abstract void InitializeSlideSensorIds();
        protected abstract (int judgeTiming, int starInLastSegmentDuration, Segment[] segments) InitializeSlideSegments();
        protected abstract IStarMovementController[] GetStars();
        protected abstract void UpdateJudgeDisplayDirection(int displaySpriteIndex);
        protected abstract SpriteRenderer GetJudgeDisplaySpriteRenderer();

        public static bool ArrowOverlapsOnSensor(SpriteRenderer slideArrowSpriteRenderer,
            NoteDataObject.SlideType slideType, Collider2D sensorCollider)
        {
            if (slideType is NoteDataObject.SlideType.RotateLeft
                or NoteDataObject.SlideType.RotateRight
                or NoteDataObject.SlideType.RotateMinorArc)
            {
                var pointResult = sensorCollider.OverlapPoint(slideArrowSpriteRenderer.transform.position);
                return pointResult;
            }

            var colliderAdded = slideArrowSpriteRenderer.TryGetComponent<BoxCollider2D>(out var addedCollider);
            if (!colliderAdded)
                addedCollider = slideArrowSpriteRenderer.gameObject.AddComponent<BoxCollider2D>();

            addedCollider.enabled = true;

            var overlapResults = new List<Collider2D>();

            var filter = new ContactFilter2D();
            filter.SetLayerMask(LayerMask.GetMask("Sensors"));

            addedCollider.Overlap(filter, overlapResults);

            Destroy(addedCollider);

            var result = overlapResults.Contains(sensorCollider);

            return result;
        }

        public static string GetMirroredSensorId(string sensorId)
        {
            if (sensorId == "C")
                return "C";
            var sensorLane = int.Parse(sensorId.Substring(1, 1));
            var sensorName = sensorId.Substring(0, 1);

            sensorLane = sensorLane switch
            {
                1 => 1,
                2 => 8,
                3 => 7,
                4 => 6,
                5 => 5,
                8 => 2,
                7 => 3,
                6 => 4,
                _ => sensorLane
            };

            return sensorName + sensorLane;
        }

        public static string GetUpdatedSensorId(string sensorId, int fromLaneIndex)
        {
            if (sensorId == "C")
                return "C";
            var sensorLane = int.Parse(sensorId.Substring(1, 1));
            var sensorName = sensorId.Substring(0, 1);
            sensorLane += fromLaneIndex;

            if (sensorLane > 8)
                sensorLane -= 8;
            else if (sensorLane < 1)
                sensorLane += 8;

            return sensorName + sensorLane;
        }

        public override void ManualUpdate()
        {
        }

        [Serializable]
        public class Segment
        {
            public enum Lane
            {
                Left,
                Center,
                Right,
                Single
            }

            public enum SensorType
            {
                Main,
                Alternative
            }

            public Sensor[] sensors;

            [HideInInspector] public SpriteRenderer[] slideSpriteRenderers;
            [HideInInspector] public SpriteRenderer[] slideSpriteRenderersWithinSensorArea;
            [HideInInspector] public SpriteRenderer[] slideSpriteRenderersOutsideSensorArea;

            public bool tapped;
            public bool touched;
            public bool arrowInBetweenConcealed;

            public bool canBeSkipped = true;

            [Serializable]
            public class Sensor
            {
                public string sensor;
                public Lane lane = Lane.Single;
                public SensorType type = SensorType.Main;
            }
        }
    }
}