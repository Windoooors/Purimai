using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class CycleSlide : NormalSlide
    {
        private bool _isClockwise;

        protected override void InitializeSlideDirection()
        {
            _isClockwise = IsCircleClockwise(fromLaneIndex + 1, toLaneIndexes[0] + 1, slideType);

            var star = stars[0];
            star.objectRotationOffset = -18;

            if (_isClockwise)
            {
                transform.eulerAngles = new Vector3(0, 0, -45f * fromLaneIndex);
                star.flipPathY = true;
                star.pathRotation = -45f * fromLaneIndex - 45;
            }
            else
            {
                MirrorSlideSensorIds();

                transform.eulerAngles = new Vector3(0, 180, 45 + 45f * fromLaneIndex);
                star.flipPathY = false;
                star.pathRotation = -45f * fromLaneIndex;
            }
        }

        protected override void UpdateJudgeDisplayDirection(int judgeDisplaySpriteGroupIndex)
        {
            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is >= 265 and <= 365 or >= -5 and <= 95;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance
                .slideJudgeDisplaySprites[judgeDisplaySpriteGroupIndex]
                .circleSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? _isClockwise ? 2 : 0
                        : _isClockwise
                            ? 3
                            : 1];
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

            var isClockwise = IsCircleClockwise(fromLane, toLane, slideType);

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

        private static bool IsCircleClockwise(int fromLane, int toLane,
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
                    return true;
            }
        }
    }
}