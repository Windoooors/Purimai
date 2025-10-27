using ChartManagement;
using UnityEngine;

namespace Game.Notes.Slides
{
    public class PqSlide : NormalSlide
    {
        protected override void InitializeSlideDirection()
        {
            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            IsClockwise = slideType == NoteDataObject.SlideDataObject.SlideType.P;

            if (IsClockwise)
            {
                transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));
                flipPathY = false;
                pathRotation = -45f * fromLaneIndex;
            }
            else
            {
                MirrorSlideSensorIds();

                transform.eulerAngles = new Vector3(0, 180, 45 + 45f * fromLaneIndex);
                flipPathY = true;
                pathRotation = -45f * fromLaneIndex - 45;
            }
        }
    }
}