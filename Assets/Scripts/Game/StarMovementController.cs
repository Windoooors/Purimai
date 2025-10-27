using Game.Notes;
using Game.Notes.Slides;
using UnityEngine;

namespace Game
{
    public class StarMovementController : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;

        public SlideBasedNote slideBasedNote;

        public string wifiSvgAssetNameOverride;

        [HideInInspector] public float duration = 5f;

        private bool _isReturning;
        private bool _moving;

        private float _time;

        private VectorGraphicsUtility _vectorGraphicsUtility;

        public void Start()
        {
            if (slideBasedNote is not WifiSlide)
                _vectorGraphicsUtility = slideBasedNote.VectorGraphicsUtility;
            else
                _vectorGraphicsUtility = new VectorGraphicsUtility(wifiSvgAssetNameOverride,
                    slideBasedNote.pathRotation, slideBasedNote.flipPathY,
                    Lanes.Instance.endPoints[slideBasedNote.fromLaneIndex].position,
                    slideBasedNote.starObjectRotationOffset);
        }

        private void Update()
        {
            if (!_moving)
                return;

            var deltaTime = Time.deltaTime;
            _time += (_isReturning ? -1 : 1) * deltaTime;

            var t = Mathf.Clamp01(_time / duration);

            if (t >= 1f) _moving = false;

            Move(t);
        }

        private void Move(float progress)
        {
            var nextPositionRotationPair = _vectorGraphicsUtility.GetPositionRotationPair(progress);

            transform.position = nextPositionRotationPair.position;
            transform.rotation = nextPositionRotationPair.rotation;
        }

        public void MoveToStart()
        {
            _vectorGraphicsUtility.SetStartPosition(transform.position);
            Move(0.001f); // 0.001f is for fixing some bizarre start point rotation issues.
            _time = duration * 0.001f;
        }

        public void StartMoving()
        {
            _moving = true;
        }

        public void StopMoving()
        {
            _moving = false;
        }
    }
}