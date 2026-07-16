using Game.ChartManagement;

namespace Game.Notes.NormalIndividualSlides
{
    public class PqSlide : IndividualSlideBase
    {
        public override void InitializeSlideDirection()
        {
            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            IsClockwise = individualSlideDataObject.Type == NoteDataObject.SlideType.P;

            if (IsClockwise)
            {
                flipPathY = false;
                pathRotation = -45f * (individualSlideDataObject.From - 1);
            }
            else
            {
                //MirrorSlideSensorIds();

                flipPathY = true;
                pathRotation = -45f * (individualSlideDataObject.From - 1) - 45;
            }
        }
    }
}