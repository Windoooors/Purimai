using Game.ChartManagement;
using UnityEngine;

namespace Game.Notes.Slides
{
    public class BigPqSlide : NormalSlide
    {
        protected override void InitializeSlideDirection()
        {
            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            IsClockwise = slideType == NoteDataObject.SlideDataObject.SlideType.BigP;
            
            if (IsClockwise)
            {
                flipPathY = false;
                pathRotation = -45f * fromLaneIndex;
            }
            else
            {
                MirrorSlideSensorIds();
                
                flipPathY = true;
                pathRotation = -45f * fromLaneIndex - 45;
            }
        }
    }
}