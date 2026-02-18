using Game.Notes;
using Game.Notes.SlideBasedNotes;
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

        private VectorGraphicsUtility _vectorGraphicsUtility;

        public void Initialize()
        {
            if (slideBasedNote is not WifiSlide)
            {
                _vectorGraphicsUtility = slideBasedNote.VectorGraphicsUtility;
            }
            else
            {
                _vectorGraphicsUtility = new VectorGraphicsUtility(wifiSvgAssetNameOverride,
                    slideBasedNote.pathRotation, slideBasedNote.flipPathY,
                    Lanes.Instance.endPoints[slideBasedNote.fromLaneIndex].position,
                    slideBasedNote.starObjectRotationOffset);
                _vectorGraphicsUtility.SetStartPosition(Lanes.Instance.endPoints[slideBasedNote.fromLaneIndex]
                    .position);
            }
        }

        public void Move(float progress)
        {
            var nextPositionRotationPair = _vectorGraphicsUtility.GetPositionRotationPair(progress);

            transform.position = nextPositionRotationPair.position;
            transform.rotation = nextPositionRotationPair.rotation;
        }
    }
}