using UnityEngine;

namespace Notes.Slides
{
    public class BigVSlide : SlideBasedNote
    {
        protected override void InitializeSlideDirection()
        {
            var star = stars[0];
            var isClockwise = IsClockWise(fromLaneIndex + 1, toLaneIndexes[0] + 1, toLaneIndexes[1] + 1);
            transform.Rotate(isClockwise
                ? new Vector3(0, 180, 45 * fromLaneIndex + 45f)
                : new Vector3(0, 0, -45 * fromLaneIndex));

            star.objectRotationOffset = -18;

            if (isClockwise)
            {
                MirrorSlideSensorIds();
                
                star.flipPathY = true;
                star.pathRotation = -45f * fromLaneIndex - 45;
            }
            else
            {
                star.flipPathY = false;
                star.pathRotation = -45f * fromLaneIndex;
            }
        }

        private static Vector2 GetPoint(int index)
        {
            var angle = 360f / 8f * (index - 1);
            var rad = Mathf.Deg2Rad * angle;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private static bool IsClockWise(int from, int middle, int to)
        {
            var a = GetPoint(from);
            var b = GetPoint(middle);
            var c = GetPoint(to);

            var ab = b - a;
            var bc = c - b;

            return Vector3.Cross(ab, bc).z > 0;
        }
    }
}