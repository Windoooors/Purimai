using Game.ChartManagement;

namespace Game.Notes.SlideBasedNotes
{
    public class BigPqSlide : NormalSlide
    {
        protected override void InitializeSlideDirection()
        {
            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            IsClockwise = slideType == NoteDataObject.SlideDataObject.SlideType.BigP;

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