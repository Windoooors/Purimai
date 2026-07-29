using Game.ChartManagement;
using Game.Notes;
using Game.Notes.NormalIndividualSlides;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{

    public class IndividualStarMovementController : StarMovementControllerBase
    {
        [FormerlySerializedAs("slideBasedNote")] public IndividualSlideBase individualSlide;

        public VectorGraphicsUtility GetGraphicsUtility() => individualSlide.GraphicsUtility;
        
        public override void Initialize()
        {
            VectorGraphicsUtility = individualSlide.GraphicsUtility;
            
            if (individualSlide is not (CycleSlide or PqSlide or BigPqSlide or LineSlide))
                VectorGraphicsUtility.FindTurningPoints();
        }
    }
}