using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.InGame
{
    public class CircleMaskManager : MonoBehaviour
    {
        public VisualTreeAsset circleMaskTreeAsset;
        private VisualElement _circleMaskTree;
        
        private void Awake()
        {
            _circleMaskTree = circleMaskTreeAsset.Instantiate();
            
            _circleMaskTree.style.position = Position.Absolute;
            _circleMaskTree.style.top = 0;
            _circleMaskTree.style.left = 0;
            _circleMaskTree.style.right = 0;
            _circleMaskTree.style.bottom = 0;
            
            UIManager.GetInstance().uiDocument.rootVisualElement.Add(_circleMaskTree);
            
            _circleMaskTree.SendToBack();
        }

        private void OnDestroy()
        {
            UIManager.GetInstance().uiDocument.rootVisualElement.Remove(_circleMaskTree);
        }
    }
}