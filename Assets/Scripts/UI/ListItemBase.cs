using UnityEngine;

namespace UI
{
    public class ListItemBase : MonoBehaviour
    {
        [HideInInspector] public RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public virtual void Bind(ItemDataBase data)
        {
        }

        public class ItemDataBase
        {
        }
    }
}