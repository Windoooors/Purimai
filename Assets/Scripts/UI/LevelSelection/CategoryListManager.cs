using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.LevelSelection
{
    public class CategoryListManager : MonoBehaviour
    {
        private const int VirtualCount = 70000;

        private static CategoryListManager _instance;

        public VisualTreeAsset itemTemplate;
        private ListView _categoryList;

        private VisualElement _categoryPanel;
        private VisualElement _controlPanel;
        private CategoryData[] _data;

        private int _lastSelectedIndex;

        private CategoryData[] _rawData;
        private ScrollView _scrollView;
        private SnapScrollManipulator _snapManipulator;

        public EventHandler<ChangeCategoryEventArgs> OnCategoryTendsToChange;

        public static CategoryListManager Instance =>
            _instance == null
                ? FindObjectsByType<CategoryListManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)[^1]
                : _instance;

        private void Awake()
        {
            _instance = this;

            LevelLoader.Instance.PlayerPrefsSavingProcedure += () =>
            {
                var index = _categoryList.selectedIndex % _rawData.Length;

                PlayerPrefs.SetInt("CategoryListIndex", index);
            };
        }

        private void Update()
        {
            _categoryList?.Query<TemplateContainer>().ForEach(SetSingleItemStyle);
        }

        private void OnDestroy()
        {
            _scrollView.RemoveManipulator(_snapManipulator);
        }

        private void OnApplicationQuit()
        {
            var index = _categoryList.selectedIndex % _rawData.Length;

            PlayerPrefs.SetInt("CategoryListIndex", index);
        }

        public void Initialize()
        {
            _controlPanel = LevelSelectionManager.Instance.LevelSelectionTree.Q<VisualElement>("control-panel");

            _categoryPanel = _controlPanel.Q<VisualElement>("category-panel");

            _categoryList = _categoryPanel.Q<ListView>("category-list");
            _scrollView = _categoryList.Q<ScrollView>();

            _categoryList.fixedItemHeight = 44;

            _categoryList.makeItem = () => itemTemplate.Instantiate();
            _categoryList.bindItem = (element, index) =>
            {
                element.Q<VisualElement>("category-item").Q<Label>("category-name-label").text =
                    _data[index].CategoryNameEntryString;

                SetSingleItemStyle(element);
                element.style.display = DisplayStyle.Flex;
            };

            _categoryList.unbindItem = (element, index) =>
            {
                element.Q<VisualElement>("category-item").Q<Label>("category-name-label").text = "";
                element.style.display = DisplayStyle.None;
            };

            _scrollView.verticalScroller.valueChanged += _ =>
            {
                _categoryList.Query<TemplateContainer>().ForEach(SetSingleItemStyle);
            };

            _snapManipulator = new SnapScrollManipulator(44, 162);

            _snapManipulator.OnSnapToItem += (sender, args) =>
            {
                if (args.IsByHand) ChangeCategoryActively(args.TargetIndex);

                _categoryList.selectedIndex = args.TargetIndex;
            };

            _scrollView.AddManipulator(_snapManipulator);
        }

        private void ChangeCategoryActively(int index)
        {
            var targetIndex = index % _rawData.Length;

            var e = new ChangeCategoryEventArgs
            {
                TargetItem = _rawData[targetIndex].FirstItem,
                Direction = index > _lastSelectedIndex ? 1 : -1
            };

            _lastSelectedIndex = index;

            OnCategoryTendsToChange?.Invoke(this, e);
        }

        public void ChangeCategoryPassively(CategoryData target)
        {
            var targetRawIndex = _rawData.ToList().IndexOf(target);

            _snapManipulator.SnapToNearest(0, targetRawIndex, _categoryList.selectedIndex, _rawData.Length, _scrollView,
                out var targetIndex);
            _categoryList.selectedIndex = targetIndex;
        }

        public void ChangeData(CategoryData[] categories)
        {
            _rawData = categories;

            var dataList = new List<CategoryData>();

            while (dataList.Count < VirtualCount) dataList.AddRange(_rawData);

            _data = dataList.ToArray();

            _categoryList.itemsSource = _data;

            _snapManipulator.SnapToItem(VirtualCount / 2, _scrollView, false, true, false);

            _lastSelectedIndex = _categoryList.selectedIndex;
        }

        private void SetSingleItemStyle(VisualElement item)
        {
            var relativeY = item.layout.y - _scrollView.verticalScroller.value;
            var distance = Mathf.Abs(relativeY - 162);
            item.style.opacity = Mathf.Clamp01(1 - distance / 115);
        }

        public class ChangeCategoryEventArgs : EventArgs
        {
            public int Direction;
            public LevelListItemData TargetItem;
        }
    }
}