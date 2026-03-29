using Game.ChartManagement;

namespace Game.Notes.NormalSlideBasedNotes
{
    public class PqIndividualSlide : NormalIndividualSlide
    {
        protected override void InitializeSlideDirection()
        {
            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            IsClockwise = slideType == NoteDataObject.SlideType.P;

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