using UnityEngine;

namespace Game.NoteEffects
{
    public class NoteEffectPhasingManager : MonoBehaviour
    {
        private static NoteEffectPhasingManager _instance;

        public float holdGlowingPhase;
        public float holdNormalPhase;

        public float breakSlideGlowingPhase;

        public float tapBreakGlowingPhase;

        public static NoteEffectPhasingManager Instance =>
            _instance ??= FindAnyObjectByType<NoteEffectPhasingManager>(FindObjectsInactive.Include);
    }
}