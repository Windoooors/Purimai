using Game.Notes;
using UnityEngine;

namespace Game
{
    public class StarMovementController : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;

        public IndividualSlideBase slideBasedNote;

        public string wifiSvgAssetNameOverride;

        private bool _isReturning;

        private VectorGraphicsUtility _vectorGraphicsUtility;

        public void Initialize()
        {
            //if (slideBasedNote is not (CycleSlide or PqSlide or BigPqSlide or LineSlide))
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