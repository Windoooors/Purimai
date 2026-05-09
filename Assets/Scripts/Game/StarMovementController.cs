using Game.ChartManagement;
using Game.Notes;
using Game.Notes.NormalSlideBasedNotes;
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

            if (slideBasedNote is not (CycleSlide or PqSlide or BigPqSlide or LineSlide or WifiSlide))
                _vectorGraphicsUtility.FindTurningPoints();
        }

        public void Move(float progress)
        {
            var nextPositionRotationPair = _vectorGraphicsUtility.GetPositionRotationPair(progress, true);

            transform.position = nextPositionRotationPair.position;
            transform.rotation = nextPositionRotationPair.rotation;
        }
    }
}