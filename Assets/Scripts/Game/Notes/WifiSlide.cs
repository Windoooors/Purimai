using System;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using UnityEngine;

namespace Game.Notes
{
    public class WifiSlide : SlideBasedNote
    {
        public Segment[] wifiSegments;
        public int slideArrowCount;
        public string svgAssetPath;
        
        public NoteDataObject.IndividualSlideDataObject slideData;
        
        private VectorGraphicsUtility _vectorGraphicsUtility;
        
        private StarMovementController _starMovementController;
        
        private void GenerateSlideArrows()
        {
            
        }

        protected override int GenerateSlideArrowObjects()
        {
            GenerateSlideArrows();
            return slideArrowCount;
        }

        protected override void InitializeVectorGraphicsUtility()
        {
            
        }

        protected override void InitializePathRotation()
        {
            
        }
    }
}