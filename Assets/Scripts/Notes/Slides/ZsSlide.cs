using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class ZsSlide : SlideBasedNote
    {
        public int toLaneIndex;

        protected override void LateStart()
        {
            var isMirror = slideType == NoteDataObject.SlideDataObject.SlideType.Z;

            transform.Rotate(
                isMirror
                    ? new Vector3(0, 180, 45 + 45f * fromLaneIndex)
                    : new Vector3(0, 0, -45f * fromLaneIndex));
        }
    }
}