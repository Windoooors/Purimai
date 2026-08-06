using Game.Notes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    public class WifiStarMovementController : StarMovementControllerBase, IStarMovementController
    {
        public WifiSlide wifiSlide;

        [FormerlySerializedAs("wifiSvgAssetNameOverride")]
        public string wifiSvgAssetName;

        public void SetStarOrder(int order)
        {
            spriteRenderer.sortingOrder -= order;
        }

        public override void Initialize()
        {
            VectorGraphicsUtility = new VectorGraphicsUtility(wifiSvgAssetName,
                wifiSlide.pathRotation, false,
                Lanes.Instance.endPoints[wifiSlide.slideData.From - 1].position,
                StarObjectRotationOffset);
            VectorGraphicsUtility.SetStartPosition(Lanes.Instance.endPoints[wifiSlide.slideData.From - 1]
                .position);
        }

        public SpriteRenderer GetSpriteRenderer()
        {
            return spriteRenderer;
        }
    }
}