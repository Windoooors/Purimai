using UnityEngine;

namespace Notes.Slides
{
    public class WifiSlide : SlideBasedNote
    {
        public int toLaneIndex;

        protected override void LateStart()
        {
            transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));

            foreach (var star in stars) star.pathRotation = -45f * fromLaneIndex;
        }
    }
}