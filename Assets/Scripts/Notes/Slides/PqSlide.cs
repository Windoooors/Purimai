using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class PqSlide : SlideBasedNote
    {
        public int toLaneIndex;

        protected override void LateStart()
        {
            var isClockwise = slideType == NoteDataObject.SlideDataObject.SlideType.P;

            if (!isClockwise)
                transform.eulerAngles = new Vector3(0, 180, 45 + 45f * fromLaneIndex);
            else
                transform.Rotate(new Vector3(0, 0, -45f * fromLaneIndex));
        }
    }
}