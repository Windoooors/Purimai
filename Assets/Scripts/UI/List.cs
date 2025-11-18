using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using LitMotion;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    public class ListEventArgs : EventArgs
    {
        public readonly int Index;

        public readonly bool IndexChangeIsAnimated;

        public ListEventArgs(int index, bool indexChangeIsAnimated)
        {
            Index = index;
            IndexChangeIsAnimated = indexChangeIsAnimated;
        }
    }

    public class List : UIScriptWithAnimation
    {
        [FormerlySerializedAs("titleListItemPrefab")]
        public TitleListItem titleItemPrefab;

        public float spacingBetweenListItems = 30;
        public int index;

        public string indexPreferenceName;

        public readonly List<ListItemBase> ItemObjectList = new();

        private RectTransform _contentRoot;
        private Vector2 _contentRootPosition;

        private ListItemBase _firstItem;
        private int _holdDirection;
        private float _holdTime;

        private bool _isHolding;

        private bool _isSelectingByHolding;
        private ListItemBase _lastItem;

        private float _normalItemHeight;
        private float _titleItemHeight;

        private int _viewIndex;

        public EventHandler<ListEventArgs> OnItemSelected;

        private void Update()
        {
            if (_isHolding)
                _holdTime += Time.deltaTime;

            if (_holdTime > 0.5f && !_isSelectingByHolding)
            {
                _isSelectingByHolding = true;
                StartCoroutine(RepeatedlySelect());
            }
        }

        private void OnEnable()
        {
            SimulatedSensor.OnTap += (_, args) =>
            {
                if (args.SensorId == "A1")
                {
                    MoveSelection(-1);
                    StartHoldingUp();
                }
                else if (args.SensorId == "A4")
                {
                    MoveSelection(1);
                    StartHoldingDown();
                }

                SimulatedSensor.OnLeave += OnLeave;
            };
        }

        public void MoveTo(int targetIndex, bool animated = true)
        {
            var delta = targetIndex - index;
            var down = delta > 0;
            for (var i = 0; i < Math.Abs(delta); i++) MoveSelection(down ? 1 : -1, animated, false);

            if (ItemObjectList[targetIndex] is TitleListItem)
                MoveSelection(1, false);
        }

        private IEnumerator RepeatedlySelect()
        {
            while (_isSelectingByHolding)
            {
                yield return new WaitForSeconds(0.15f);
                MoveSelection(_holdDirection);
            }
        }

        private void StartHoldingDown()
        {
            if (_isHolding)
                return;

            _isHolding = true;
            _holdDirection = 1;
        }

        private void StartHoldingUp()
        {
            if (_isHolding)
                return;

            _isHolding = true;
            _holdDirection = -1;
        }

        private void EndHoldingDown()
        {
            if (!_isHolding || _holdDirection != 1) return;

            _isHolding = false;
            _holdTime = 0;
            _isSelectingByHolding = false;
        }

        private void EndHoldingUp()
        {
            if (!_isHolding || _holdDirection != -1) return;

            _isHolding = false;
            _holdTime = 0;
            _isSelectingByHolding = false;
        }

        private void OnLeave(object sender, TouchEventArgs args)
        {
            if (args.SensorId == "A4")
                EndHoldingDown();
            else if (args.SensorId == "A1")
                EndHoldingUp();

            SimulatedSensor.OnLeave -= OnLeave;
        }

        public void Initialize(ItemDataBase[] allData, ListItemBase normalItemPrefab)
        {
            var top = 0f;

            index = 0;
            _viewIndex = 0;

            _contentRoot = GetComponent<RectTransform>();

            _contentRoot.anchoredPosition = new Vector2(_contentRoot.anchoredPosition.x, 0);

            _contentRootPosition = _contentRoot.anchoredPosition;
            _normalItemHeight = normalItemPrefab.GetComponent<RectTransform>().sizeDelta.y;
            _titleItemHeight = titleItemPrefab.GetComponent<RectTransform>().sizeDelta.y;

            foreach (var obj in ItemObjectList) Destroy(obj.gameObject);
            ItemObjectList.Clear();

            for (var i = 0; i < (allData.Length < 8 ? 8 : 1); i++)
                foreach (var data in allData)
                {
                    var isTitle = data is TitleListItem.TitleData;

                    var generatedItemObject = Instantiate(isTitle ? titleItemPrefab : normalItemPrefab, transform);

                    generatedItemObject.Bind(data);

                    var rectTransform = generatedItemObject.GetComponent<RectTransform>();

                    rectTransform.anchoredPosition = new Vector2(0,
                        top - (isTitle ? _titleItemHeight / 2 : _normalItemHeight / 2) + _normalItemHeight / 2);

                    top -= (isTitle ? _titleItemHeight : _normalItemHeight) + spacingBetweenListItems;

                    generatedItemObject.Deselect();

                    ItemObjectList.Add(generatedItemObject);
                }

            top = spacingBetweenListItems;
            // Flip the last four items onto the above of the first item.
            for (var i = 1; i <= 4; i++)
            {
                var generatedItemObject = ItemObjectList[^i];
                var isTitle = generatedItemObject is TitleListItem;
                var rectTransform = generatedItemObject.GetComponent<RectTransform>();

                rectTransform.anchoredPosition = new Vector2(0,
                    top + (isTitle ? _titleItemHeight / 2 : _normalItemHeight / 2) + _normalItemHeight / 2);
                top += (isTitle ? _titleItemHeight : _normalItemHeight) + spacingBetweenListItems;
            }

            _firstItem = ItemObjectList[^4];
            _lastItem = ItemObjectList[^5];

            if (ItemObjectList.Count > 1)
            {
                var lastSavedIndex = PlayerPrefs.GetInt(indexPreferenceName, 1);
                MoveTo(lastSavedIndex > ItemObjectList.Count - 1 ? 1 : lastSavedIndex, false);
            }
        }

        private void MoveSelection(int direction, bool animated = true, bool ignoreTitleItem = true)
        {
            _contentRootPosition = Move(_contentRootPosition);

            if (ItemObjectList[index] is TitleListItem && ignoreTitleItem)
                _contentRootPosition = Move(_contentRootPosition);

            if (animated)
                AddMotionHandle(LMotion
                    .Create(_contentRoot.anchoredPosition, _contentRootPosition, 0.5f)
                    .WithEase(Ease.OutExpo)
                    .Bind(x => { _contentRoot.anchoredPosition = x; }
                    ));
            else
                _contentRoot.anchoredPosition = _contentRootPosition;

            return;

            Vector2 Move(Vector2 contentRootPosition)
            {
                var lastIndex = index;

                _viewIndex += direction;
                index = Mod(_viewIndex, ItemObjectList.Count);

                PlayerPrefs.SetInt(indexPreferenceName, index);

                var result = contentRootPosition + direction * new Vector2(0,
                    spacingBetweenListItems + (ItemObjectList[direction == 1 ? lastIndex : index] is TitleListItem
                        ? _titleItemHeight
                        : _normalItemHeight));

                OnItemSelected?.Invoke(this, new ListEventArgs(index, animated));

                ItemObjectList[index].Select();
                ItemObjectList[lastIndex].Deselect();

                switch (direction)
                {
                    case 1:
                        var temporaryLastItem = _lastItem;
                        _lastItem = _firstItem;
                        _firstItem =
                            ItemObjectList[Mod(ItemObjectList.IndexOf(_firstItem) + 1, ItemObjectList.Count)];

                        _lastItem.rectTransform.anchoredPosition = temporaryLastItem.rectTransform.anchoredPosition -
                                                                   new Vector2(0,
                                                                       _lastItem.rectTransform.sizeDelta.y / 2 +
                                                                       temporaryLastItem.rectTransform.sizeDelta.y / 2
                                                                       + spacingBetweenListItems);
                        break;
                    case -1:
                        var temporaryFirstItem = _firstItem;
                        _firstItem = _lastItem;
                        _lastItem = ItemObjectList[Mod(ItemObjectList.IndexOf(_lastItem) - 1, ItemObjectList.Count)];

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