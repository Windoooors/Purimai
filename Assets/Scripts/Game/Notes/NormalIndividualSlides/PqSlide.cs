using Game.ChartManagement;
using UnityEngine;

namespace Game.Notes.NormalIndividualSlides
{
    public class PqSlide : IndividualSlideBase
    {
        public override void InitializeSlideDirection()
        {
            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            IsClockwise = IndividualSlideDataObject.Type == NoteDataObject.SlideType.P;

            if (IsClockwise)
            {
                flipPathY = false;
                pathRotation = -45f * (IndividualSlideDataObject.From - 1);
            }
            else
            {
                MirrorSlideSensorIds();

                flipPathY = true;
                pathRotation = -45f * (IndividualSlideDataObject.From - 1) - 45;
            }

            transform.Rotate(new Vector3(0, 0, -45f * (IndividualSlideDataObject.From - 1)));
        }
    }
}