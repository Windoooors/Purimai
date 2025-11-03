using System.Collections.Generic;
using Game;
using LitMotion;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    public class List : MonoBehaviour
    {
        [FormerlySerializedAs("titleListItemPrefab")]
        public TitleListItem titleItemPrefab;

        public float spacingBetweenListItems = 30;
        public int index;

        public ListItemBase itemObject;

        private readonly List<ListItemBase> _itemObjectList = new();
        private RectTransform _contentRoot;

        private ListItemBase _firstItem;
        private ListItemBase _lastItem;

        private float _normalItemHeight;
        private float _titleItemHeight;

        private int _viewIndex;

        private void Start()
        {
            var data = new ListItemBase.ItemDataBase[]
            {
                new TitleListItem.TitleData { CategoryName = "Chapter 1" },
                new LevelListItem.LevelListItemData { LevelName = "Item A" },
                new LevelListItem.LevelListItemData { LevelName = "Item B" },
                new TitleListItem.TitleData { CategoryName = "Chapter 2" },
                new LevelListItem.LevelListItemData { LevelName = "Item C" },
                new LevelListItem.LevelListItemData { LevelName = "Item D" }
            };

            SimulatedSensor.OnTap += (_, args) =>
            {
                if (args.SensorId == "A1")
                    MoveSelection(-1);
                else if (args.SensorId == "A4") MoveSelection(1);
            };

            Initialize(data, itemObject);
        }

        public void Initialize(ListItemBase.ItemDataBase[] allData, ListItemBase normalItemPrefab)
        {
            var top = 0f;

            _contentRoot = GetComponent<RectTransform>();
            _contentRootPosition = _contentRoot.anchoredPosition;
            _normalItemHeight = normalItemPrefab.GetComponent<RectTransform>().sizeDelta.y;
            _titleItemHeight = titleItemPrefab.GetComponent<RectTransform>().sizeDelta.y;

            for (var i = 0; i < (allData.Length < 9 ? 9 : 1); i++)
                foreach (var data in allData)
                {
                    var isTitle = data is TitleListItem.TitleData;

                    var generatedItemObject = Instantiate(isTitle ? titleItemPrefab : normalItemPrefab, transform);
                    
                    generatedItemObject.Bind(data);

                    var rectTransform = generatedItemObject.GetComponent<RectTransform>();

                    rectTransform.anchoredPosition = new Vector2(0,
                        top - (isTitle ? _titleItemHeight / 2 : _normalItemHeight / 2) + _normalItemHeight / 2);

                    top -= (isTitle ? _titleItemHeight : _normalItemHeight) + spacingBetweenListItems;

                    _itemObjectList.Add(generatedItemObject);
                }

            top = spacingBetweenListItems;
            // Flip the last four items onto the above of the first item.
            for (var i = 1; i <= 4; i++)
            {
                var generatedItemObject = _itemObjectList[^i];
                var isTitle = generatedItemObject is TitleListItem;
                var rectTransform = generatedItemObject.GetComponent<RectTransform>();

                rectTransform.anchoredPosition = new Vector2(0,
                    top + (isTitle ? _titleItemHeight / 2 : _normalItemHeight / 2) + _normalItemHeight / 2);
                top += (isTitle ? _titleItemHeight : _normalItemHeight) + spacingBetweenListItems;
            }

            _firstItem = _itemObjectList[^4];
            _lastItem = _itemObjectList[^5];
        }


        private MotionHandle _currentMotion;
        private Vector2 _contentRootPosition;
        private void MoveSelection(int direction, bool animated = true)
        {
            _contentRootPosition = Move(_contentRootPosition);

            if (_itemObjectList[index] is TitleListItem)
                _contentRootPosition = Move(_contentRootPosition);

            _currentMotion.TryCancel();

            _currentMotion = LMotion
                .Create(_contentRoot.anchoredPosition, _contentRootPosition, animated ? 0.5f : 0).WithEase(Ease.OutExpo).Bind(x =>
                    {
                        _contentRoot.anchoredPosition = x;
                    }
                );

            return;

            Vector2 Move(Vector2 contentRootPosition)
            {
                var lastIndex = index;
                _viewIndex += direction;
                index = Mod(_viewIndex, _itemObjectList.Count);
                var result = contentRootPosition + direction * new Vector2(0,
                    30 + (_itemObjectList[direction == 1 ? lastIndex : index] is TitleListItem
                        ? _titleItemHeight
                        : _normalItemHeight));

                switch (direction)
                {
                    case 1:
                        var temporaryLastItem = _lastItem;
                        _lastItem = _firstItem;
                        _firstItem =
                            _itemObjectList[Mod(_itemObjectList.IndexOf(_firstItem) + 1, _itemObjectList.Count)];

                        _lastItem.rectTransform.anchoredPosition = temporaryLastItem.rectTransform.anchoredPosition -
                                                                   new Vector2(0,
                                                                       _lastItem.rectTransform.sizeDelta.y / 2 +
                                                                       temporaryLastItem.rectTransform.sizeDelta.y / 2
                                                                       + spacingBetweenListItems);
                        break;
                    case -1:
                        var temporaryFirstItem = _firstItem;
                        _firstItem = _lastItem;
                        _lastItem = _itemObjectList[Mod(_itemObjectList.IndexOf(_lastItem) - 1, _itemObjectList.Count)];

                        _firstItem.rectTransform.anchoredPosition = temporaryFirstItem.rectTransform.anchoredPosition +
                                                                    new Vector2(0,
                                                                        _firstItem.rectTransform.sizeDelta.y / 2 +
                                                                        temporaryFirstItem.rectTransform.sizeDelta.y /
                                                                        2 + spacingBetweenListItems);
                        break;
                }

                return result;

                int Mod(int a, int b)
                {
                    return (a % b + b) % b;
                }
            }
        }
    }
}