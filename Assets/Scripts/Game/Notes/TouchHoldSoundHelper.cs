using System.Collections.Generic;
using Game.Notes.TouchBasedNotes;
using UnityEngine;

namespace Game.Notes
{
    public class TouchHoldSoundHelper : MonoBehaviour
    {
        private static TouchHoldSoundHelper _instance;
        private readonly HashSet<TouchHold> _callers = new();

        public static TouchHoldSoundHelper Instance => _instance ??= FindAnyObjectByType<TouchHoldSoundHelper>();

        public void Play(TouchHold caller)
        {
            _callers.Add(caller);

            SfxManager.Instance.ResetAndPlayTouchHoldSound();
        }

        public void Stop(TouchHold caller)
        {
            _callers.Remove(caller);

            if (_callers.Count == 0) SfxManager.Instance.StopTouchHoldSound();
        }
    }
}