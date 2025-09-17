using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class ZsSlide : NormalSlide
    {
        protected override void InitializeSlideDirection()
        {
            var isMirror = slideType == NoteDataObject.SlideDataObject.SlideType.Z;

            transform.Rotate(
                isMirror
                    ? new Vector3(0, 180, 45 + 45f * fromLaneIndex)
                    : new Vector3(0, 0, -45f * fromLaneIndex));

            var star = stars[0];
            if (isMirror)
            {
                MirrorSlideSensorIds();

                star.flipPathY = true;
                star.pathRotation = -45f * fromLaneIndex - 45;
            }
            else
            {
                star.flipPathY = false;
                star.pathRotation = -45f * fromLaneIndex;
            }
            
            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is > 270 and <= 360 or > 0 and <= 90;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance.slideJudgeDisplaySprites[0]
                .normalSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? isMirror ? 0 : 1
                        : isMirror
                            ? 1
                            : 0];
                
            var scale = judgeDisplaySpriteRenderer.gameObject.transform.localScale;
            scale = new Vector3(scale.x, judgeSpriteNeedsChange ? scale.y : -scale.y, scale.z);
            judgeDisplaySpriteRenderer.transform.localScale = scale;
        }
    }
}