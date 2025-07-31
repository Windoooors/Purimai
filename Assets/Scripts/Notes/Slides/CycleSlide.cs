using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class CycleSlide : SlideBasedNote
    {
        public int toLaneIndex;
        //public int interval;

        protected override void LateStart()
        {
            var isClockwise = IsClockwise(fromLaneIndex + 1, toLaneIndex + 1, slideType);

            var star = stars[0];
            star.objectRotationOffset = -18;
            star.pathDirection = StarMovementController.PathDirection.StartToEnd;

            if (isClockwise)
            {
                transform.eulerAngles = new Vector3(0, 0, -45f * fromLaneIndex);
                star.flipPathY = true;
                star.pathRotation = -45f * fromLaneIndex - 45;
            }
            else
            {
                transform.eulerAngles = new Vector3(0, 180, 45 + 45f * fromLaneIndex);
                star.flipPathY = false;
                star.pathRotation = -45f * fromLaneIndex;
            }
        }

        private static bool IsUpper(int point)
        {
            return point == 1 || point == 2 || point == 7 || point == 8;
        }

        public static int GetCycleInterval(int fromLane, int toLane, NoteDataObject.SlideDataObject.SlideType slideType)
        {
            if (fromLane == toLane) return 8;

            if (slideType == NoteDataObject.SlideDataObject.SlideType.RotateMinorArc)
                return GetShortestInterval(fromLane, toLane);

            var isClockwise = IsClockwise(fromLane, toLane, slideType);

            var interval = 0;
            var current = fromLane;

            while (true)
            {
                current = isClockwise ? current % 8 + 1 : current == 1 ? 8 : current - 1;
                interval++;
                if (current == toLane)
                    break;
            }

            return interval;
        }

        private static bool IsClockwise(int fromLane, int toLane,
            NoteDataObject.SlideDataObject.SlideType directionType)
        {
            switch (directionType)
            {
                case NoteDataObject.SlideDataObject.SlideType.RotateLeft:
                    return !IsUpper(fromLane);
                case NoteDataObject.SlideDataObject.SlideType.RotateRight:
                    return IsUpper(fromLane);
                case NoteDataObject.SlideDataObject.SlideType.RotateMinorArc:
                    if (fromLane == toLane)
                        return true;
                    var clockwiseInterval = (toLane - fromLane + 8) % 8;
                    var counterClockwiseInterval = (fromLane - toLane + 8) % 8;
                    return clockwiseInterval <= counterClockwiseInterval;
                default:
                    Debug.Log("Fuck");
                    return true;
            }
        }
    }
}