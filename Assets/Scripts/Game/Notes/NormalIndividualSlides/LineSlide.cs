using UnityEngine;

namespace Game.Notes.NormalIndividualSlides
{
    public class LineSlide : IndividualSlideBase
    {
        public override void InitializeSlideDirection()
        {
            IsClockwise = true;

            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            transform.Rotate(new Vector3(0, 0, -45f * (IndividualSlideDataObject.From - 1)));

            pathRotation = -45f * (IndividualSlideDataObject.From - 1);
        }
    }
}