using Game.Notes;
using Game.Notes.SlideBasedNotes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    public class StarMovementController : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;

        [FormerlySerializedAs("slideBasedNote")] public IndividualSlideBase individualSlideBase;

        public string wifiSvgAssetNameOverride;

        [HideInInspector] public float duration = 5f;

        private bool _isReturning;

        private VectorGraphicsUtility _vectorGraphicsUtility;

        public void Initialize()
        {
            if (individualSlideBase is not WifiSlide)
            {
                _vectorGraphicsUtility = individualSlideBase.VectorGraphicsUtility;
            }
            else
            {
                _vectorGraphicsUtility = new VectorGraphicsUtility(wifiSvgAssetNameOverride,
                    individualSlideBase.pathRotation, individualSlideBase.flipPathY,
                    Lanes.Instance.endPoints[individualSlideBase.fromLaneIndex].position,
                    individualSlideBase.starObjectRotationOffset);
                _vectorGraphicsUtility.SetStartPosition(Lanes.Instance.endPoints[individualSlideBase.fromLaneIndex]
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