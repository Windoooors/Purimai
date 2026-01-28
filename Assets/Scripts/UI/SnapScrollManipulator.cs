using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Mathematics;
using LitMotion;

namespace UI
{
    public class SnapScrollManipulator : Manipulator
    {
        private readonly float _itemHeight;
        private readonly float _offset;
        private const float VelocityThreshold = 300f;

        private ScrollView _scrollView;
        private float _lastY;
        private float _currentVelocity;

        private IVisualElementScheduledItem _monitorTask;

        private float _scrollDecelerationRate;

        private bool _isPointerDown;

        public SnapScrollManipulator(float itemHeight, float offset)
        {
            _itemHeight = itemHeight;
            _offset = offset;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            _scrollView = target as ScrollView;
            if (_scrollView == null) return;

            _scrollDecelerationRate = _scrollView.scrollDecelerationRate;

            _scrollView.RegisterCallback<PointerDownEvent>(_ =>
            {
                _scrollView.scrollDecelerationRate = _scrollDecelerationRate;
                _isPointerDown = true;
                _lastY = _scrollView.scrollOffset.y;
                StopSnapping();
            });

            _scrollView.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                _currentVelocity = (_scrollView.scrollOffset.y - _lastY) / Time.deltaTime;
                
                _lastY = _scrollView.scrollOffset.y;
                
                _isPointerDown = false;

                if (_currentVelocity == 0)
                {
                    int targetIndex = Mathf.RoundToInt((_scrollView.scrollOffset.y + _offset) / _itemHeight);
                
                    SnapToItem(targetIndex, _scrollView);
                }
            });

            _monitorTask = _scrollView.schedule.Execute(OnFrameUpdate).Every(0);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            _monitorTask?.Pause();
        }

        private void OnFrameUpdate()
        {
            var currentY = _scrollView.scrollOffset.y;
            var dt = Time.deltaTime;

            if (dt > 0)
            {
                _currentVelocity = (currentY - _lastY) / dt;
                _lastY = currentY;
            }

            if (!_isPointerDown && math.abs(_currentVelocity) < VelocityThreshold && _currentVelocity != 0 && !_animating)
            {
                int targetIndex = _currentVelocity > 0
                    ? Mathf.CeilToInt((currentY + _offset) / _itemHeight)
                    : Mathf.FloorToInt((currentY + _offset) / _itemHeight);
                
                SnapToItem(targetIndex, _scrollView);
            }
            else if (!_animating)
            {
                _scrollView.scrollDecelerationRate = _scrollDecelerationRate;
            }
        }

        private bool _animating;

        private MotionHandle _motionHandle;

        private void StopSnapping()
        {
            _animating = false;
            _motionHandle.TryCancel();
        }

        public void SnapToItem(int targetIndex, ScrollView scrollView, bool animated = true)
        {
            if (_animating)
                return;
            
            _scrollView.scrollDecelerationRate = 0;
            
            _animating = true;

            var targetY = targetIndex * _itemHeight - _offset;

            var startValue = scrollView.verticalScroller.value;

            OnSnapToItem?.Invoke(this, new SnapToItemEventArgs()
            {
                TargetIndex = targetIndex
            });
            
            if (animated)
                _motionHandle = LMotion.Create(startValue, targetY, 0.5f).WithOnComplete(() =>
                {
                    _animating = false;
                }).WithEase(Ease.OutExpo).Bind(x => scrollView.verticalScroller.value = x);
            else
            {
                scrollView.verticalScroller.value = targetY;
                _animating = false;
            }
        }

        public class SnapToItemEventArgs : EventArgs
        {
            public int TargetIndex { get; set; }
        }

        public EventHandler<SnapToItemEventArgs> OnSnapToItem;
    }
}