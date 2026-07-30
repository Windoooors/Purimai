using UnityEngine;

namespace Game
{
    public interface IStarMovementController
    {
        public void Move(float progress);
        public void Initialize();
        public SpriteRenderer GetSpriteRenderer();
        public void SetStarOrder(int order);
    }

    public abstract class StarMovementControllerBase : MonoBehaviour
    {
        public const float StarObjectRotationOffset = -18;
        public SpriteRenderer spriteRenderer;

        private bool _isReturning;

        protected VectorGraphicsUtility VectorGraphicsUtility { get; set; }

        public abstract void Initialize();

        public void Move(float progress)
        {
            var nextPositionRotationPair = VectorGraphicsUtility.GetPositionRotationPair(progress, true);

            transform.position = nextPositionRotationPair.position;
            transform.rotation = nextPositionRotationPair.rotation;
        }
    }
}