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

            var star = stars[0];

            if (IsClockwise)
            {
                transform.eulerAngles = new Vector3(0, 0, -45f * fromLaneIndex);

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