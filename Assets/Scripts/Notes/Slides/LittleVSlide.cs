using UnityEngine;

namespace Notes.Slides
{
    public class LittleVSlide : SlideBasedNote
    {
        protected override void LateStart()
        {
            transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));

            var star = stars[0];
            star.pathRotation = -45f * fromLaneIndex;
        }
    }
}