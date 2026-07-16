using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Notes
{
    public abstract class SlideBasedNote : NoteBase
    {
        [HideInInspector] public Segment[] segments;

        protected int order;
        public int timing;
        public bool isEach;
        public bool suddenlyAppears;

        public void Initialize(NoteDataObject.SlideDataObject slideDataObject,bool isSlideEach, int noteTiming, ref int slideArrowOrder)
        {
            order = -slideArrowOrder;
            timing = noteTiming;
            isEach = isSlideEach;
            suddenlyAppears = slideDataObject.SuddenlyAppears;
            transform.position = Vector3.zero;

            InitializePathRotation();
            InitializeVectorGraphicsUtility();
            
            slideArrowOrder -= GenerateSlideArrowObjects();
        }

        protected abstract int GenerateSlideArrowObjects();
        protected abstract void InitializeVectorGraphicsUtility();
        protected abstract void InitializePathRotation();
        
        public override void ManualUpdate()
        {
            
        }

        public override void AddAutoPlayKeyFrame()
        {
            
        }
        
        [Serializable]
        public class Segment
        {
            public Sensor[] sensors;

            [HideInInspector] public SpriteRenderer[] slideSpriteRenderers;
            public SpriteRenderer[] slideSpriteRenderersWithinSensorArea;
            public SpriteRenderer[] slideSpriteRenderersOutsideSensorArea;

            public bool tapped;
            public bool touched;
            public bool arrowInBetweenConcealed;

            public bool canBeSkipped = true;

            [Serializable]
            public class Sensor
            {
                public string sensor;
                public SensorType type;
            }

            public enum SensorType
            {
                Main,
                L,
                M,
                R,
                Other
            }
        }
    }
}