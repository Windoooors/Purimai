using UnityEngine;

namespace Game.Notes.Slides
{
    public class BigVSlide : NormalSlide
    {
        protected override void InitializeSlideDirection()
        {
            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            IsClockwise = IsClockWise(fromLaneIndex + 1, toLaneIndexes[0] + 1, toLaneIndexes[1] + 1);

            if (IsClockwise)
            {
                MirrorSlideSensorIds();

                flipPathY = true;
                pathRotation = -45f * fromLaneIndex - 45;
            }
            else
            {
                flipPathY = false;
                pathRotation = -45f * fromLaneIndex;
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