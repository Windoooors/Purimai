using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class ZsSlide : SlideBasedNote
    {
        protected override void LateStart()
        {
            var isMirror = slideType == NoteDataObject.SlideDataObject.SlideType.Z;

            transform.Rotate(
                isMirror
                    ? new Vector3(0, 180, 45 + 45f * fromLaneIndex)
                    : new Vector3(0, 0, -45f * fromLaneIndex));

            var star = stars[0];
            if (isMirror)
            {
                star.flipPathY = true;
                star.pathRotation = -45f * fromLaneIndex - 45;
            }
            else
            {
                star.flipPathY = false;
                star.pathRotation = -45f * fromLaneIndex;
            }
        }
    }
}