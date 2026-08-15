using Unity.Mathematics;
using UnityEngine;

namespace Game.NoteEffects
{
    public class NoteEffectPhasingManager : MonoBehaviour
    {
        private const float TotalDuration = 0.417f;

        private const float TapPeak = 1.5f;
        private const int TapRepeatTimes = 2;

        private const float HoldPeak = 1f;
        private const int HoldRepeatTimes = 3;

        private const float SlidePeak = 0.5f;
        private const int SlideRepeatTimes = 1;
        private static NoteEffectPhasingManager _instance;
        private float _holdDuration;
        private float _slideDuration;
        private float _tapDuration;

        private float _time;

        public float HoldGlowingPhase { get; private set; }
        public float HoldNormalPhase { get; private set; }

        public float BreakSlideGlowingPhase { get; private set; }

        public float TapBreakGlowingPhase { get; private set; }

        public static NoteEffectPhasingManager Instance =>
            _instance ??= FindAnyObjectByType<NoteEffectPhasingManager>(FindObjectsInactive.Include);

        private void Awake()
        {
            _instance = this;

            _holdDuration = TotalDuration / HoldRepeatTimes;
            _slideDuration = TotalDuration / SlideRepeatTimes;
            _tapDuration = TotalDuration / TapRepeatTimes;
        }

        private void Update()
        {
            _time += Time.deltaTime;

            var phase = _time % TotalDuration;

            BreakSlideGlowingPhase = math.abs(phase % _slideDuration / _slideDuration - 0.5f) * 2 * SlidePeak;
            TapBreakGlowingPhase = math.abs(phase % _tapDuration / _tapDuration - 0.5f) * 2 * TapPeak;
            HoldGlowingPhase = math.abs(phase % _holdDuration / _holdDuration - 0.5f) * 2 * HoldPeak;
            HoldNormalPhase = 0;
        }
    }
}