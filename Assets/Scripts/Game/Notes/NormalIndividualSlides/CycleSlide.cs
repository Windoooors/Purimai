using Game.ChartManagement;
using UnityEngine;

namespace Game.Notes.NormalIndividualSlides
{
    public class CycleSlide : IndividualSlideBase
    {
        public override void UpdateJudgeDisplayDirection(int judgeDisplaySpriteGroupIndex)
        {
            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.position.y < 0;

            var index = judgeSpriteNeedsChange
                ? IsClockwise ? 3 : 1
                : IsClockwise
                    ? 2
                    : 0;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance
                .slideJudgeDisplaySprites[judgeDisplaySpriteGroupIndex]
                .circleSlideJudgeSprites[index];

            if (!judgeSpriteNeedsChange) judgeDisplaySpriteRenderer.transform.eulerAngles += new Vector3(0, 0, 180);

            judgeDisplaySpriteRenderer.transform.eulerAngles += new Vector3(0, 0, IsClockwise ? 200f : -20f);
        }

        public override void InitializeSlideDirection()
        {
            IsClockwise = IsCircleClockwise(IndividualSlideDataObject.From, IndividualSlideDataObject.To[0],
                IndividualSlideDataObject.Type);

            var fromLaneIndex = IndividualSlideDataObject.From - 1;

            if (IsClockwise)
            {
                flipPathY = true;
                pathRotation = -45f * fromLaneIndex - 45;
            }
            else
            {
                MirrorSlideSensorIds();

                flipPathY = false;
                pathRotation = -45f * fromLaneIndex;
            }

            transform.Rotate(new Vector3(0, 0, -45f * (IndividualSlideDataObject.From - 1)));
        }

        private static bool IsUpper(int point)
        {
            return point == 1 || point == 2 || point == 7 || point == 8;
        }

        public static int GetCycleInterval(int fromLane, int toLane, NoteDataObject.SlideType slideType)
        {
            if (fromLane == toLane) return 8;

            if (slideType == NoteDataObject.SlideType.RotateMinorArc)
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
            NoteDataObject.SlideType directionType)
        {
            switch (directionType)
            {
                case NoteDataObject.SlideType.RotateLeft:
                    return !IsUpper(fromLane);
                case NoteDataObject.SlideType.RotateRight:
                    return IsUpper(fromLane);
                case NoteDataObject.SlideType.RotateMinorArc:
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