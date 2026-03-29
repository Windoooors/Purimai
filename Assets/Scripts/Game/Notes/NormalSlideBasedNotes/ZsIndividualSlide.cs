using Game.ChartManagement;

namespace Game.Notes.NormalSlideBasedNotes
{
    public class ZsIndividualSlide : NormalIndividualSlide
    {
        private bool _isMirror;

        protected override void InitializeSlideDirection()
        {
            _isMirror = slideType == NoteDataObject.SlideType.Z;

            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            if (_isMirror)
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
    }
}