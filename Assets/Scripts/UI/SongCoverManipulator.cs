using System;
using UnityEngine.UIElements;

namespace UI
{
    public class SongCoverManipulator : Manipulator
    {
        public enum SongCoverLayoutPopulationMode
        {
            FixedHeight,
            MinimalLeft
        }

        private readonly SongCoverLayoutPopulationMode _mode;
        private readonly int _widthOffset;

        private VisualElement _cover;

        private StyleLength _originalWidth;

        public EventHandler<GeometryChangedEventArgs> OnGeometryChanged;

        public SongCoverManipulator(SongCoverLayoutPopulationMode mode, int widthOffset)
        {
            _mode = mode;
            _widthOffset = widthOffset;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            _cover = target;
            if (_cover == null) return;

            _originalWidth = _cover.style.width;

            _cover.RegisterCallback<GeometryChangedEvent>(UpdatePosition);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetached);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            _cover.UnregisterCallback<GeometryChangedEvent>(UpdatePosition);
        }

        private void OnDetached(DetachFromPanelEvent evt)
        {
            target.RemoveManipulator(this);
        }

        private void UpdatePosition(GeometryChangedEvent evt)
        {
            switch (_mode)
            {
                case SongCoverLayoutPopulationMode.FixedHeight:
                    FixedHeight(evt);
                    break;
                case SongCoverLayoutPopulationMode.MinimalLeft:
                    MinimalLeft(evt);
                    break;
            }
        }

        private void MinimalLeft(GeometryChangedEvent evt)
        {
            var width = _cover.layout.width;
            var freeWidth = UIManager.Instance.uiDocument.rootVisualElement.layout.width - _cover.layout.xMin +
                            _widthOffset;

            if (width <= freeWidth)
            {
                _cover.style.width = freeWidth;
                OnGeometryChanged?.Invoke(this, new GeometryChangedEventArgs
                {
                    NewBackgroundSize = new BackgroundSize(freeWidth, freeWidth)
                });
                //_cover.style.top = (UIManager.Instance.uiDocument.rootVisualElement.layout.height - freeWidth) / 2;
            }
        }

        private void FixedHeight(GeometryChangedEvent evt)
        {
            var width = _cover.layout.width;
            var originalHeight = _cover.layout.height;

            if (width <= originalHeight)
            {
                _cover.style.width = originalHeight;
                width = originalHeight;
            }
            else
            {
                _cover.style.width = _originalWidth;
            }

            _cover.style.height = width;
            _cover.style.top = _cover.style.top.value.value - (width - originalHeight) / 2;
        }

        public class GeometryChangedEventArgs : EventArgs
        {
            public BackgroundSize NewBackgroundSize;
        }
    }
}