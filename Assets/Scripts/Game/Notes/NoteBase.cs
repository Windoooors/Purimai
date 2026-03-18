using UnityEngine;

namespace Game.Notes
{
    public abstract class NoteBase : MonoBehaviour
    {
        public int emergingTime;

        public abstract void ManualUpdate();

        public abstract void AddAutoPlayKeyFrame();
    }
}