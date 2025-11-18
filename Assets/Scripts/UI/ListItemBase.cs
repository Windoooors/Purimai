using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ListItemBase : UIScriptWithAnimation
    {
        [HideInInspector] public RectTransform rectTransform;
        [HideInInspector] public Image backgroundImage;

        //private Color _backgroundColor;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            //backgroundImage = GetComponent<Image>();
            //_backgroundColor = backgroundImage.color;
        }

        public void Select()
        {
            ProcessSelect();
        }

        public void Deselect()
        {
            ProcessDeselect();
        }

        public virtual void ProcessDeselect()
        {
        }

        public virtual void ProcessSelect()
        {
        }

        public virtual void Bind(ItemDataBase data)
        {
        }
    }

    public class ItemDataBase
    {
    }
}