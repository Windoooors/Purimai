using LitMotion;
using UnityEngine;

namespace Notes.Taps
{
    public class Tap : TapBasedNote
    {
        public bool isStarHead;
        public bool isNoSpinningStarHead;
        public bool isBreak;

        public SpriteRenderer tapSpriteRenderer;
        public Transform tapTransform;

        private bool _emerging;
        private bool _moving;

        public void Update()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            if (ChartPlayer.Instance.time > timing - 2 * EmergingDuration &&
                ChartPlayer.Instance.time < timing - 1 * EmergingDuration && !_emerging)
            {
                _emerging = true;

                transform.position = Vector3.zero;

                LMotion.Create(0, 1f, EmergingDuration / 1000f).WithEase(Ease.OutSine)
                    .Bind(x =>
                    {
                        tapSpriteRenderer.color = new Color(1, 1, 1, x);
                        lineSpriteRenderer.color = new Color(1, 1, 1, x);
                    });
                LMotion.Create(0, 1f, EmergingDuration / 1000f).WithEase(Ease.Linear)
                    .Bind(x => tapTransform.localScale = x * Vector3.one);
            }

            if (ChartPlayer.Instance.time > timing - 1 * EmergingDuration && _emerging && !_moving)
            {
                _emerging = false;
                _moving = true;
            }

            if (ChartPlayer.Instance.time > timing + 100)
            {
                _moving = false;
                transform.position = NoteGenerator.Instance.outOfScreenPosition;
            }

            if (_moving)
            {
                tapTransform.localScale = Vector3.one;
                tapTransform.Translate(Speed * Time.deltaTime * Vector3.up);

                lineTransform.localScale += LineExpansionSpeed * Time.deltaTime * Vector3.one;
            }
        }

        protected override void LateStart()
        {
            transform.position = Vector3.zero;
            tapTransform.localScale = Vector3.zero;
            tapTransform.position *= NoteGenerator.Instance.originCircleScale;
            tapSpriteRenderer.color = new Color(1, 1, 1, 0);
            transform.position = NoteGenerator.Instance.outOfScreenPosition;
        }
    }
}