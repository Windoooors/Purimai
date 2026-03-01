using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UI.Settings.Managers
{
    public class SettingsManager : SettingsManagerBase
    {
        private static SettingsManager _instance;

        public static Action OnSettingsChanged;

        public VisualTreeAsset settingsTreeAsset;
        public VisualTreeAsset tabContentTreeAsset;

        private VisualElement _root;

        private VisualElement _settingsTree;
        private TabView _tabView;

        public static SettingsManager Instance => _instance ??=
            FindObjectsByType<SettingsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)[^1];

        private void Awake()
        {
            _instance = this;

            LocalizationTableName = "UI.Settings";

            Initialize();
        }

        private void OnDestroy()
        {
            UIManager.Instance.uiDocument?.rootVisualElement?.Remove(_settingsTree);
        }

        private void Initialize()
        {
            _root = UIManager.Instance.uiDocument.rootVisualElement;

            _settingsTree = settingsTreeAsset.Instantiate();

            _settingsTree.style.position = Position.Absolute;
            _settingsTree.style.left = 0;
            _settingsTree.style.right = 0;
            _settingsTree.style.top = 0;
            _settingsTree.style.bottom = 0;

            _root.Add(_settingsTree);

            _tabView = _settingsTree.Q("transparent-panel").Q<TabView>();

            var clickablePanel = _settingsTree.Q("transparent-panel").Q("clickable-panel");

            clickablePanel.RegisterCallback<PointerUpEvent>(_ =>
            {
                SettingsPool.Save();

                OnSettingsChanged?.Invoke();

                ClosePanel();
            });

            foreach (var category in SettingsItems.Settings)
            {
                var tab = new Tab();

                var titleLocalizedString = new LocalizedString(LocalizationTableName, $"settings.{category.Identifier}");

                titleLocalizedString.StringChanged += value => { tab.label = value; };

                titleLocalizedString.RefreshString();

                var tabContent = tabContentTreeAsset.Instantiate();

                tab.Add(tabContent);

                tabContent.style.position = Position.Absolute;
                tabContent.style.top = 0;
                tabContent.style.bottom = 0;
                tabContent.style.left = 0;
                tabContent.style.right = 0;

                var listView = tabContent.Q<ListView>();

                listView.TrySetTouchDraggingAllowed(true);

                SetUpList(listView, category.Items.ToArray());

                _tabView.contentViewport.BringToFront();

                _tabView.Add(tab);
            }

            _settingsTree.style.display = DisplayStyle.None;
            
            StartCoroutine(Show());
        }

        private IEnumerator Show()
        {
            yield return new WaitForSeconds(0.1f);
            _settingsTree.style.display = DisplayStyle.Flex;
            _settingsTree.AddToClassList("shown");
        }

        private void ClosePanel()
        {
            _settingsTree.RemoveFromClassList("shown");
            _settingsTree.AddToClassList("hidden");

            Invoke(nameof(DestroySelf), 0.5f);
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}