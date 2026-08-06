using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game
{
    public class ChainedStarMovementController : IStarMovementController
    {
        private readonly List<float> _lastJunctionPositions = new();
        private int _lastSegmentIndex = -1;

        public ChainedStarMovementController(IndividualStarMovementController[] individualStarMovementControllers)
        {
            IndividualStarMovementControllers = individualStarMovementControllers;
        }

        public IndividualStarMovementController[] IndividualStarMovementControllers { get; }

        public void SetStarOrder(int order)
        {
            foreach (var individualStarMovementController in IndividualStarMovementControllers)
                individualStarMovementController.spriteRenderer.sortingOrder -= order;
        }

        public void Initialize()
        {
            foreach (var individualStarMovementController in IndividualStarMovementControllers)
            {
                individualStarMovementController.Initialize();

                individualStarMovementController.GetGraphicsUtility().ObjectRotationOffset =
                    StarMovementControllerBase.StarObjectRotationOffset;
            }

            var totalLength = IndividualStarMovementControllers.Sum(x => x.GetGraphicsUtility().GetTotalLength());

            foreach (var individualStarMovementController in IndividualStarMovementControllers)
                _lastJunctionPositions.Add(
                    individualStarMovementController.GetGraphicsUtility().GetTotalLength() / totalLength +
                    (_lastJunctionPositions.Count > 0 ? _lastJunctionPositions[^1] : 0));

            foreach (var individualStarMovementController in IndividualStarMovementControllers)
                individualStarMovementController.gameObject.SetActive(false);
        }

        public SpriteRenderer GetSpriteRenderer()
        {
            return IndividualStarMovementControllers[_lastSegmentIndex + 1].spriteRenderer;
        }

        public void Move(float progress)
        {
            if (_lastSegmentIndex == -1 && !IndividualStarMovementControllers[0].gameObject.activeSelf)
                IndividualStarMovementControllers[0].gameObject.SetActive(true);

            while (_lastSegmentIndex != _lastJunctionPositions.Count - 2 &&
                   progress > _lastJunctionPositions[_lastSegmentIndex + 1])
            {
                _lastSegmentIndex++;

                if (_lastSegmentIndex != -1)
                    IndividualStarMovementControllers[_lastSegmentIndex].gameObject.SetActive(false);
                IndividualStarMovementControllers[_lastSegmentIndex + 1].gameObject.SetActive(true);
            }

            var lastJunctionPosition = _lastSegmentIndex == -1 ? 0 : _lastJunctionPositions[_lastSegmentIndex];
            var nextJunctionPosition = _lastJunctionPositions[_lastSegmentIndex + 1];

            var individualProgress = (progress - lastJunctionPosition) /
                                     (nextJunctionPosition - lastJunctionPosition);

            IndividualStarMovementControllers[_lastSegmentIndex + 1].Move(individualProgress);
        }
    }
}