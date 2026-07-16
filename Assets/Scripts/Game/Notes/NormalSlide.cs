using System;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using UnityEngine;

namespace Game.Notes
{
    public class NormalSlide : SlideBasedNote
    {
        [HideInInspector]
        public List<IndividualSlideBase> individualSlides = new ();
        
        private StarMovementController _starMovementController;
        
        protected override int GenerateSlideArrowObjects()
        {
            var count = 0;
            
            foreach (var individualSlideBase in individualSlides)
            {
                count += individualSlideBase.GenerateSlideArrows();
            }
            
            return count;
        }

        protected override void InitializeVectorGraphicsUtility()
        {
            foreach (var individualSlideBase in individualSlides)
            {
                individualSlideBase.InitializeVectorGraphicsUtility();
            }
        }

        protected override void InitializePathRotation()
        {
            foreach (var individualSlideBase in individualSlides)
            {
                individualSlideBase.InitializeSlideDirection();   
            }
        }
    }
}