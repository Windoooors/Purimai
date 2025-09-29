using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class PqSlide : NormalSlide
    {
        protected override void InitializeSlideDirection()
        {
            slideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            IsClockwise = slideType == NoteDataObject.SlideDataObject.SlideType.P;

            var star = stars[0];

            if (IsClockwise)
            {
                transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));
                star.flipPathY = false;
                star.pathRotation = -45f * fromLaneIndex;
            }
            else
            {
                MirrorSlideSensorIds();

                transform.eulerAngles = new Vector3(0, 180, 45 + 45f * fromLaneIndex);
                star.flipPathY = true;
                star.pathRotation = -45f * fromLaneIndex - 45;
            }
        }
    }
}