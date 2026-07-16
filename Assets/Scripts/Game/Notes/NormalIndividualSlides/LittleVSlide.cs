using UnityEngine;

namespace Game.Notes.NormalIndividualSlides
{
    public class LittleVSlide : IndividualSlideBase
    {
        public override void InitializeSlideDirection()
        {
            IsClockwise = true;

            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            transform.Rotate(new Vector3(0, 0, -45f * (individualSlideDataObject.From - 1)));

            pathRotation = -45f * (individualSlideDataObject.From - 1);
        }
    }
}