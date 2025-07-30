using ChartManagement;
using LitMotion;
using Unity.Mathematics;
using UnityEngine;

namespace Notes
{
    public abstract class SlideBasedNote : MonoBehaviour
    {
        public NoteDataObject.SlideDataObject.SlideType slideType;

        public int fromLaneIndex;
        public int timing;
        public int waitDuration;
        public int slideDuration;

        public bool isEach;
        public int order;

        public bool isWifi;

        public SpriteRenderer[] slideSpriteRenderers;
        private bool _concealed;

        private bool _revealed;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            isWifi = slideType == NoteDataObject.SlideDataObject.SlideType.Wifi;

            var i = 0;
            foreach (var slideSpriteRenderer in slideSpriteRenderers)
            {
                if (isEach)
                    if (isWifi)
                    {
                        slideSpriteRenderer.sprite = NoteGenerator.Instance.wifiSlideEachSprites[i];
                        i++;
                    }
                    else
                    {
                        slideSpriteRenderer.sprite = NoteGenerator.Instance.slideEachSprite;
                    }

                slideSpriteRenderer.sortingOrder += order;
            }

            transform.position = NoteGenerator.Instance.outOfScreenPosition;

            LateStart();
        }

        // Update is called once per frame
        private void Update()
        {
            if (ChartPlayer.Instance.time >= timing + ChartPlayer.Instance.starAppearanceDelay && !_revealed)
            {
                transform.position = Vector3.zero;
                
                foreach (var spriteRenderer in slideSpriteRenderers)
                {
                    LMotion.Create(0, 1f, ChartPlayer.Instance.starAppearanceDuration / 1000f).WithEase(Ease.Linear)
                        .Bind(x => spriteRenderer.color = new Color(1, 1, 1, x));
                }

                _revealed = true;
            }

            if (ChartPlayer.Instance.time >= timing + waitDuration + slideDuration && !_concealed)
            {
                //foreach (var spriteRenderer in slideSpriteRenderers) spriteRenderer.enabled = false;

                transform.position = NoteGenerator.Instance.outOfScreenPosition;
                
                _concealed = true;
            }
        }

        protected virtual void LateStart()
        {
        }

        public static int GetShortestInterval(int fromLane, int toLane)
        {
            if (fromLane == toLane) return 0;

            var clockwiseInterval = (toLane - fromLane + 8) % 8;
            var counterClockwiseInterval = (fromLane - toLane + 8) % 8;

            return math.min(clockwiseInterval, counterClockwiseInterval);
        }

        public static (int clockwiseInterval, int counterClockwiseInterval) GetIntervalInBothWays(int start, int end)
        {
            var clockwise = (end - start + 8) % 8;

            var counterClockwise = (start - end + 8) % 8;

            return (clockwise, counterClockwise);
        }
    }
}