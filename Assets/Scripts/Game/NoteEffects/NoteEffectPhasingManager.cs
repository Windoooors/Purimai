using UnityEngine;

namespace Game.NoteEffects
{
    public class NoteEffectPhasingManager : MonoBehaviour
    {
        public static NoteEffectPhasingManager Instance =>
            _instance ??= FindAnyObjectByType<NoteEffectPhasingManager>(FindObjectsInactive.Include);
        private static NoteEffectPhasingManager _instance;

        public float holdGlowingPhase;
        public float holdNormalPhase;
        
        public float breakSlideGlowingPhase;

        public float tapBreakGlowingPhase;
    }
}