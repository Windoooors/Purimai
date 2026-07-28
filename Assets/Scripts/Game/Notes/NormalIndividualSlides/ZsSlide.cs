using Game.ChartManagement;
using UnityEngine;

namespace Game.Notes.NormalIndividualSlides
{
    public class ZsSlide : IndividualSlideBase
    {
        private bool _isMirror;

        public override void InitializeSlideDirection()
        {
            _isMirror = individualSlideDataObject.Type == NoteDataObject.SlideType.Z;

            SlideJudgeDisplaySpriteIndexes = new[] { 0, 1 };

            if (_isMirror)
            {
                MirrorSlideSensorIds();

                flipPathY = true;
                pathRotation = -45f * (individualSlideDataObject.From - 1) - 45;
            }
            else
            {
                flipPathY = false;
                pathRotation = -45f * (individualSlideDataObject.From - 1);
            }
            
            transform.Rotate(new Vector3(0, 0, -45f * (individualSlideDataObject.From - 1)));
        }
    }
}