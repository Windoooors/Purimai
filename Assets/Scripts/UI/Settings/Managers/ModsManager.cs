using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Settings.Managers
{
    public class ModsManager : SettingsManagerBase
    {
        public static Action OnModsChanged;

        private static ModsManager _instance;

        public VisualTreeAsset modsTreeAsset;
        private VisualElement _modsTree;

        private VisualElement _root;
        public static ModsManager Instance => _instance ?? FindAnyObjectByType<ModsManager>();

        private void Awake()
        {
            _instance = this;

            Initialize();
        }

        private void OnDestroy()
        {
            UIManager.Instance.uiDocument?.rootVisualElement?.Remove(_modsTree);
        }

        private void Initialize()
        {
            _root = UIManager.Instance.uiDocument.rootVisualElement;

            _modsTree = modsTreeAsset.Instantiate();

            _root.Add(_modsTree);

            _modsTree.style.position = Position.Absolute;
            _modsTree.style.left = 0;
            _modsTree.style.right = 0;
            _modsTree.style.top = 0;
            _modsTree.style.bottom = 0;

            var clickablePanel = _modsTree.Q("transparent-panel").Q("clickable-panel");

            clickablePanel.RegisterCallback<PointerUpEvent>(_ =>
            {
                SettingsPool.Save();

                OnModsChanged?.Invoke();

                ClosePanel();
            });

            var listView = _modsTree.Q<ListView>();

            listView.TrySetTouchDraggingAllowed(true);

            SetUpList(listView, SettingsItems.ModItems);

            StartCoroutine(Show());
        }

        private IEnumerator Show()
        {
            yield return new WaitForSeconds(0.1f);
            _modsTree.style.display = DisplayStyle.Flex;
            _modsTree.AddToClassList("shown");
            yield break;
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }

        private void ClosePanel()
        {
            _modsTree.RemoveFromClassList("shown");
            _modsTree.AddToClassList("hidden");

            Invoke(nameof(DestroySelf), 0.5f);
        }
    }
}