using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    public class ListItemBase : UIScriptWithAnimation
    {
        [HideInInspector] public RectTransform rectTransform;

        [FormerlySerializedAs("dataIndex")] public int indexOnScreen = -1;
        public bool shownOnScreen;
        [HideInInspector] public DecodedImage backgroundImage;
        public ItemDataBase Data;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            //backgroundImage = GetComponent<Image>();
            //_backgroundColor = backgroundImage.color;
            LateAwake();
        }

        protected virtual void LateAwake()
        {
        }

        //private Color _backgroundColor;

        public void Allocate(int index)
        {
            if (shownOnScreen && index != -1)
                return;

            indexOnScreen = index;
            shownOnScreen = true;
        }

        public void Deallocate()
        {
            indexOnScreen = -1;
            shownOnScreen = false;

            ProcessDeallocate();
        }

        public void Select(bool animated = true)
        {
            ProcessSelect(animated);
        }

        public void Deselect(bool animated = true)
        {
            ProcessDeselect(animated);
        }

        public virtual void ProcessDeselect(bool animated = true)
        {
        }

        public virtual void ProcessSelect(bool animated = true)
        {
        }

        public virtual void ProcessDeallocate()
        {
        }

        public void Bind(ItemDataBase data)
        {
            Data = data;

            ProcessBind();
        }

        public virtual void ProcessBind()
        {
        }
    }

    public class ItemDataBase
    {
    }
}