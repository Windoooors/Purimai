using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ListItemBase : MonoBehaviour
    {
        [HideInInspector] public RectTransform rectTransform;
        [HideInInspector] public Image backgroundImage;
        
        private Color _backgroundColor;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            backgroundImage = GetComponent<Image>();
            _backgroundColor = backgroundImage.color;
        }

        public void Select()
        {
            LMotion.Create(0, 1f, 0.2f).Bind(x => backgroundImage.color = new Color(1, 1, 1, x));
            ProcessSelect();
        }

        public void Deselect()
        {
            LMotion.Create(backgroundImage.color, _backgroundColor, 0.2f).Bind(x => backgroundImage.color = x);
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