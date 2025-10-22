using ChartManagement;
using UnityEngine;

namespace Notes.Slides
{
    public class ZsSlide : NormalSlide
    {
        private bool _isMirror;

        protected override void InitializeSlideDirection()
        {
            _isMirror = slideType == NoteDataObject.SlideDataObject.SlideType.Z;

            transform.Rotate(
                _isMirror
                    ? new Vector3(0, 180, 45 + 45f * fromLaneIndex)
                    : new Vector3(0, 0, -45f * fromLaneIndex));

            var star = stars[0];
            if (_isMirror)
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
        }

        protected override void UpdateJudgeDisplayDirection(int judgeDisplaySpriteGroupIndex)
        {
            var judgeSpriteNeedsChange =
                judgeDisplaySpriteRenderer.transform.rotation.eulerAngles.z is >= 265 and <= 365 or >= -5 and <= 95;

            judgeDisplaySpriteRenderer.sprite = NoteGenerator.Instance
                .slideJudgeDisplaySprites[judgeDisplaySpriteGroupIndex]
                .normalSlideJudgeSprites[
                    judgeSpriteNeedsChange
                        ? _isMirror ? 0 : 1
                        : _isMirror
                            ? 1
                            : 0];

            var scale = judgeDisplaySpriteRenderer.gameObject.transform.localScale;
            scale = new Vector3(scale.x,
                judgeSpriteNeedsChange ? Mathf.Abs(scale.y) : -Mathf.Abs(scale.y),
                scale.z);
            judgeDisplaySpriteRenderer.transform.localScale = scale;
        }
    }
}