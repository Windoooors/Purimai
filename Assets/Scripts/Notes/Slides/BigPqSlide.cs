using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class BigPqSlide : NormalSlide
    {
        protected override void InitializeSlideDirection()
        {
            var isClockwise = slideType == NoteDataObject.SlideDataObject.SlideType.BigP;

            var star = stars[0];

            if (isClockwise)
            {
                transform.eulerAngles = new Vector3(0, 0, -45f * fromLaneIndex);

                star.flipPathY = false;
                star.pathRotation = -45f * fromLaneIndex;
            }
            else
            {
                MirrorSlideSensorIds();

                transform.eulerAngles = new Vector3(0, 180, 45 + 45f * fromLaneIndex);

                star.flipPathY = true;
                star.pathRotation = -45f * fromLaneIndex - 45;
            }

            Debug.Log(judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z);

            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is > 270 and <= 360 or > 0 and <= 90;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance.slideJudgeDisplaySprites[0]
                .normalSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? isClockwise ? 1 : 0
                        : isClockwise
                            ? 0
                            : 1];
                
            var scale = judgeDisplaySpriteRenderer.gameObject.transform.localScale;
            scale = new Vector3(scale.x, judgeSpriteNeedsChange ? scale.y : -scale.y, scale.z);
            judgeDisplaySpriteRenderer.transform.localScale = scale;
        }
    }
}