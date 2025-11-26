using Game.ChartManagement;

namespace Game.Notes.Slides
{
    public class PqSlide : NormalSlide
    {
        protected override void InitializeSlideDirection()
        {
            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            IsClockwise = slideType == NoteDataObject.SlideDataObject.SlideType.P;

            if (IsClockwise)
            {
                flipPathY = false;
                pathRotation = -45f * fromLaneIndex;
            }
            else
            {
                MirrorSlideSensorIds();

                flipPathY = true;
                pathRotation = -45f * fromLaneIndex - 45;
            }
        }
    }
}