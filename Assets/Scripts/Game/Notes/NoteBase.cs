using UnityEngine;

namespace Game.Notes
{
    public abstract class NoteBase : MonoBehaviour
    {
        protected const int HiddenLayer = 8;
        protected const int ShownLayer = 3;
        public int emergingTime;

        public abstract void ManualUpdate();

        protected void SetActive(bool active, GameObject targetObject)
        {
            targetObject.SetActive(active);

            var layer = active ? ShownLayer : HiddenLayer;

            foreach (Transform child in targetObject.transform) child.gameObject.layer = layer;

            targetObject.layer = layer;
        }

        public abstract void AddAutoPlayKeyFrame();
    }
}