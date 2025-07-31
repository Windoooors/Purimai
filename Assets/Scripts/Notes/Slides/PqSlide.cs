using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class PqSlide : SlideBasedNote
    {
        public int toLaneIndex;

        protected override void LateStart()
        {
            var isClockwise = slideType == NoteDataObject.SlideDataObject.SlideType.P;

            var star = stars[0];

            if (isClockwise)
            {
                transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));
                star.flipPathY = false;
                star.pathRotation = -45f * fromLaneIndex;
            }
            else
            {
                transform.eulerAngles = new Vector3(0, 180, 45 + 45f * fromLaneIndex);
                star.flipPathY = true;
                star.pathRotation = -45f * fromLaneIndex - 45;
            }
        }
    }
}