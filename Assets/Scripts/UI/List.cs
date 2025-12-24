using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [FormerlySerializedAs("index")] public int dataIndex;

        public string indexPreferenceName;
        public int visibleItemCount = 16;

        [FormerlySerializedAs("_isHolding")] public bool isHolding;

        public readonly List<ListItemBase> ItemObjectPool = new();

        private RectTransform _contentRoot;

        private float _contentRootPositionY;

        private ListItemBase _firstItem;
        private int _holdDirection;
        private float _holdTime;

        private bool _isSelectingByHolding;
        private ListItemBase _lastItem;

        private float _normalItemHeight;
        private ListItemBase _normalItemPrefab;
        private float _titleItemHeight;

        private float _top;

        public ItemDataBase[] AllData;

        public EventHandler<ListEventArgs> OnItemSelected;

        private void Update()
        {
            if (isHolding)
                _holdTime += Time.deltaTime;

            if (_holdTime > 0.5f && !_isSelectingByHolding)
            {
                _isSelectingByHolding = true;
                StartCoroutine(RepeatedlySelect());
            }
        }

        private void OnEnable()
        {
            RegisterSimulatedSensorEvent();
        }

        private string _lastTappedSensor;

        private void ScrollSection(int direction)
        {
            _lastTappedSensor = "";
            
            var titleDataArray = AllData.Where(x => x is TitleListItem.TitleData).ToArray();
            
            if (titleDataArray.Length == 0)
                return;
            if (titleDataArray.Length == 1)
            {
                MoveTo(AllData.ToList().IndexOf(titleDataArray[0]) + 1);
                
                return;
            }

            var targetIndex = dataIndex;

            var count = 0;

            while (true)
            {
                targetIndex += direction;

                if (targetIndex < 0)
                    targetIndex = AllData.Length - 1;
                if (targetIndex >= AllData.Length)
                    targetIndex = 0;

                if (AllData[targetIndex] is TitleListItem.TitleData)
                {
                    if (direction == 1)
                        break;
                    
                    count++;
                    if (count == 2)
                        break;
                }
            }
            
            MoveTo(targetIndex + 1);
        }

        public void RegisterSimulatedSensorEvent()
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

                if (args.SensorId is "B3" or "B6" && _lastTappedSensor is "B2" or "B7")
                    ScrollSection(-1);
                else if (args.SensorId is "B2" or "B7" && _lastTappedSensor is "B3" or "B6")
                    ScrollSection(1);
                else
                    _lastTappedSensor = args.SensorId;

                SimulatedSensor.OnLeave += OnLeave;
            };
        }

        private IEnumerator RepeatedlySelect()
        {
            while (_isSelectingByHolding)
            {
                yield return new WaitForSeconds(0.1f);
                MoveSelection(_holdDirection);
            }
        }

        private void StartHoldingDown()
        {
            if (isHolding)
                return;

            isHolding = true;
            _holdDirection = 1;
        }

        private void StartHoldingUp()
        {
            if (isHolding)
                return;

            isHolding = true;
            _holdDirection = -1;
        }

        public void EndHoldingDown()
        {
            if (!isHolding || _holdDirection != 1) return;

            isHolding = false;
            _holdTime = 0;
            _isSelectingByHolding = false;
        }

        public void EndHoldingUp()
        {
            if (!isHolding || _holdDirection != -1) return;

            isHolding = false;
            _holdTime = 0;
            _isSelectingByHolding = false;
        }

        private void OnLeave(object sender, TouchEventArgs args)
        {
            if (args.SensorId == "A4")
            {
                EndHoldingDown();
                SimulatedSensor.OnLeave -= OnLeave;
            }
            else if (args.SensorId == "A1")
            {
                EndHoldingUp();
                SimulatedSensor.OnLeave -= OnLeave;
            }
        }

        public void Initialize(ItemDataBase[] data, ListItemBase normalItemPrefab)
        {
            AllData = data;
            _normalItemPrefab = normalItemPrefab;
            _contentRoot = GetComponent<RectTransform>();
            _normalItemHeight = _normalItemPrefab.GetComponent<RectTransform>().sizeDelta.y;
            _titleItemHeight = titleItemPrefab.GetComponent<RectTransform>().sizeDelta.y;

            if (data.Length == 0)
                return;

            dataIndex = PlayerPrefs.GetInt(indexPreferenceName, 1);

            if (dataIndex > AllData.Length)
                dataIndex = 1;

            if (data[dataIndex] is TitleListItem.TitleData)
                dataIndex++;

            InitializeHeight();
            InitializePool();
            BindData();

            OnItemSelected?.Invoke(this, new ListEventArgs(dataIndex, false));
        }

        public ListItemBase GetSelectedItemObject()
        {
            return ItemObjectPool.Find(x => x.indexOnScreen == visibleItemCount / 2);
        }

        private void InitializeHeight()
        {
            _top = 0;

            _contentRootPositionY = 0;

            ClearMotion(true);

            _contentRoot.anchoredPosition = new Vector2(_contentRoot.anchoredPosition.x, 0);

            for (var i = dataIndex - visibleItemCount / 2; i < dataIndex; i++)
            {
                var j = i;

                while (j < 0)
                    j += AllData.Length;
                while (j > AllData.Length - 1)
                    j -= AllData.Length;

                var data = AllData[j];

                var isTitle = data is TitleListItem.TitleData;

                _top += (isTitle ? _titleItemHeight : _normalItemHeight) + spacingBetweenListItems;
            }
        }

        private void InitializePool()
        {
            foreach (var obj in ItemObjectPool) Destroy(obj.gameObject);
            ItemObjectPool.Clear();

            for (var i = 0; i < visibleItemCount + 2; i++)
            {
                var normalItem = Instantiate(_normalItemPrefab, transform);
                var titleItem = Instantiate(titleItemPrefab, transform);

                normalItem.rectTransform.anchoredPosition = new Vector2(-5000, _top);
                titleItem.rectTransform.anchoredPosition = new Vector2(-5000, _top);

                normalItem.List = this;
                titleItem.List = this;

                ItemObjectPool.Add(normalItem);
                ItemObjectPool.Add(titleItem);
            }
        }

        private void BindData()
        {
            var indexOnScreen = 0;

            for (var i = dataIndex - visibleItemCount / 2; i < dataIndex + visibleItemCount / 2; i++)
            {
                var j = i;

                while (j < 0)
                    j += AllData.Length;
                while (j > AllData.Length - 1)
                    j -= AllData.Length;

                var data = AllData[j];

                var isTitle = data is TitleListItem.TitleData;

                var availableListItem = GetAvailableListItem(isTitle);

                availableListItem.Bind(data);
                availableListItem.Allocate(indexOnScreen++);

                var rectTransform = availableListItem.rectTransform;

                rectTransform.anchoredPosition = new Vector2(0,
                    _top - (isTitle ? _titleItemHeight / 2 : _normalItemHeight / 2) + _normalItemHeight / 2);

                _top -= (isTitle ? _titleItemHeight : _normalItemHeight) + spacingBetweenListItems;

                if (i != dataIndex)
                    availableListItem.Deselect(false);
                else
                    availableListItem.Select(false);
            }
        }

        private ListItemBase GetAvailableListItem(bool findTitle)
        {
            foreach (var itemObject in ItemObjectPool)
                if (!itemObject.shownOnScreen)
                {
                    itemObject.Deselect(false);

                    if (findTitle && itemObject is TitleListItem titleListItem)
                        return titleListItem;

                    if (!findTitle && itemObject is not TitleListItem)
                        return itemObject;
                }

            return null;
        }

        public void Rebind()
        {
            foreach (var item in ItemObjectPool)
                if (item.shownOnScreen)
                    item.ProcessBind();
        }

        private void MoveSelection(int direction, bool animated = true)
        {
            var button = Button.GetButton(direction switch
            {
                -1 => 0,
                _ => 3
            });

            button.Press();

            ItemObjectPool.Find(x => x.indexOnScreen == visibleItemCount / 2).Deselect(animated);

            var deltaTop = direction * (spacingBetweenListItems +
                                        (GetSelectedItemObject() is TitleListItem
                                            ? _titleItemHeight
                                            : _normalItemHeight));

            dataIndex += direction;
            while (dataIndex < 0)
                dataIndex += AllData.Length;
            while (dataIndex > AllData.Length - 1)
                dataIndex -= AllData.Length;

            PlayerPrefs.SetInt(indexPreferenceName, dataIndex);

            var selectedItemIsTitle = AllData[dataIndex] is TitleListItem.TitleData;

            var newItemDataIndex =
                dataIndex + direction switch { 1 => visibleItemCount / 2 - 1, -1 => -visibleItemCount / 2, _ => 0 };
            while (newItemDataIndex < 0)
                newItemDataIndex += AllData.Length;
            while (newItemDataIndex > AllData.Length - 1)
                newItemDataIndex -= AllData.Length;

            var newItemData = AllData[newItemDataIndex];

            var newItemIsTitle = newItemData is TitleListItem.TitleData;

            var newTop = 0f;

            switch (direction)
            {
                case -1:
                    var firstItem = GetFirst();

                    newTop = firstItem.rectTransform.anchoredPosition.y +
                             (firstItem is TitleListItem ? _titleItemHeight / 2 : _normalItemHeight / 2) +
                             spacingBetweenListItems;

                    DeallocateLast();

                    foreach (var item in ItemObjectPool)
                        if (item.shownOnScreen)
                            item.indexOnScreen++;

                    break;
                case 1:
                    var lastItem = GetLast();

                    newTop = lastItem.rectTransform.anchoredPosition.y -
                             (lastItem is TitleListItem ? _titleItemHeight / 2 : _normalItemHeight / 2) -
                             spacingBetweenListItems;

                    DeallocateFirst();

                    foreach (var item in ItemObjectPool)
                        if (item.shownOnScreen)
                            item.indexOnScreen--;

                    break;
            }

            var newLastItem = GetAvailableListItem(newItemIsTitle);

            newLastItem.Allocate(direction == 1 ? visibleItemCount - 1 : 0);

            newLastItem.rectTransform.anchoredPosition = new Vector2(0,
                newTop - direction * (newItemIsTitle ? _titleItemHeight / 2 : _normalItemHeight / 2));

            newLastItem.Bind(newItemData);

            ItemObjectPool.Find(x => x.indexOnScreen == visibleItemCount / 2).Select(animated);

            OnItemSelected?.Invoke(this, new ListEventArgs(dataIndex, animated));

            ClearMotion();

            _contentRootPositionY += deltaTop;

            var nowContentRootPositionY = _contentRoot.anchoredPosition.y;

            if (animated)
                AddMotionHandle(
                    LMotion.Create(nowContentRootPositionY, _contentRootPositionY, 0.5f).WithEase(Ease.OutExpo)
                        .Bind(x =>
                        {
                            _contentRoot.anchoredPosition =
                                new Vector2(_contentRoot.anchoredPosition.x, x);
                        }), false
                );
            else
                _contentRoot.anchoredPosition =
                    new Vector2(_contentRoot.anchoredPosition.x, _contentRootPositionY);

            if (selectedItemIsTitle)
                MoveSelection(direction, animated);
        }

        private ListItemBase GetLast()
        {
            return ItemObjectPool.Find(x => x.indexOnScreen == visibleItemCount - 1);
        }

        private ListItemBase GetFirst()
        {
            return ItemObjectPool.Find(x => x.indexOnScreen == 0);
        }

        private void DeallocateLast()
        {
            foreach (var item in ItemObjectPool)
                if (item.indexOnScreen == visibleItemCount - 1)
                {
                    item.Deallocate();
                    item.Deselect(false);
                    item.rectTransform.anchoredPosition =
                        new Vector2(-5000, _top);
                }
        }

        private void DeallocateFirst()
        {
            foreach (var item in ItemObjectPool)
                if (item.indexOnScreen == 0)
                {
                    item.Deallocate();
                    item.Deselect(false);
                    item.rectTransform.anchoredPosition =
                        new Vector2(-5000, _top);
                }
        }

        public void MoveTo(int targetIndex, bool animated = true)
        {
            dataIndex = targetIndex;
            PlayerPrefs.SetInt(indexPreferenceName, dataIndex);
            InitializeHeight();

            foreach (var item in ItemObjectPool)
            {
                item.Deallocate();
                item.Deselect(false);
                item.rectTransform.anchoredPosition =
                    new Vector2(-5000, _top);
            }

            BindData();

            OnItemSelected?.Invoke(this, new ListEventArgs(targetIndex, animated));
        }
    }
}