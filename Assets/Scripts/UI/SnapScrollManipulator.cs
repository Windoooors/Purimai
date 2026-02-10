using System;
using LitMotion;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class SnapScrollManipulator : Manipulator
    {
        private const float VelocityThreshold = 300f;
        private readonly float _itemHeight;
        private readonly float _offset;

        private bool _animating;

        private int _currentIndexAfterAnimation;

        private bool _isPointerDown;

        private float _lastSnappedVelocity;
        private float _lastY;

        private IVisualElementScheduledItem _monitorTask;

        private MotionHandle _motionHandle;

        private float _scrollDecelerationRate;

        private ScrollView _scrollView;

        private TouchTracker _tracker;

        private float _velocity;

        public EventHandler<SnapToItemEventArgs> OnSnapToItem;


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

            _tracker = new TouchTracker(target, () =>
            {
                _isPointerDown = true;

                _scrollView.scrollDecelerationRate = _scrollDecelerationRate;

                StopSnapping();
            }, () =>
            {
                if (!_isPointerDown)
                    return;

                _isPointerDown = false;
            });

            _monitorTask = _scrollView.schedule.Execute(OnFrameUpdate).Every(33);

            target.RegisterCallback<DetachFromPanelEvent>(OnDetached);
        }

        private void OnDetached(DetachFromPanelEvent evt)
        {
            _monitorTask?.Pause();

            _tracker.Dispose();

            target.RemoveManipulator(this);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            _monitorTask?.Pause();

            _tracker.Dispose();
        }

        private void OnFrameUpdate()
        {
            var currentY = _scrollView.scrollOffset.y;

            _velocity = (currentY - _lastY) / 33 * 1000;
            _lastY = currentY;

            if (!_isPointerDown && math.abs(_velocity) < VelocityThreshold && !_animating)
            {
                var targetIndex = Mathf.RoundToInt((currentY + _offset) / _itemHeight);

                SnapToItem(targetIndex, _scrollView);
            }
            else if (!_animating)
            {
                _scrollView.scrollDecelerationRate = _scrollDecelerationRate;
            }
        }

        private void StopSnapping()
        {
            _animating = false;
            _motionHandle.TryCancel();
        }

        public void SnapToItem(int targetIndex, ScrollView scrollView, bool animated = true, bool stopCurrent = false,
            bool byHand = true)
        {
            if (stopCurrent)
                StopSnapping();

            if (_animating)
                return;

            _scrollView.scrollDecelerationRate = 0;

            _animating = true;

            var targetY = targetIndex * _itemHeight - _offset;

            var startValue = scrollView.verticalScroller.value;

            if (animated)
            {
                _motionHandle = LMotion.Create(startValue, targetY, 0.5f).WithOnComplete(() =>
                {
                    _animating = false;
                    if (targetIndex != _currentIndexAfterAnimation)
                        OnSnapToItem?.Invoke(this,
                            new SnapToItemEventArgs { TargetIndex = targetIndex, IsByHand = byHand });

                    _currentIndexAfterAnimation = targetIndex;
                }).WithEase(Ease.OutExpo).Bind(x => scrollView.verticalScroller.value = x);
            }
            else
            {
                scrollView.verticalScroller.value = targetY;
                _animating = false;
                if (targetIndex != _currentIndexAfterAnimation)
                    OnSnapToItem?.Invoke(this,
                        new SnapToItemEventArgs { TargetIndex = targetIndex, IsByHand = byHand });

                _currentIndexAfterAnimation = targetIndex;
            }
        }

        public void SnapToNearest(int direction, int targetRawIndex, int currentIndex, int rawItemCount,
            ScrollView scrollView, out int targetIndex, bool byHand = false, bool animated = true)
        {
            var index = currentIndex % rawItemCount;

            var resultIndex = currentIndex;

            resultIndex -= index;

            if (direction != 0)
            {
                resultIndex += direction == 1 ? targetRawIndex : targetRawIndex - rawItemCount;
            }
            else
            {
                var doubleUpwards = resultIndex + rawItemCount + targetRawIndex;
                var upwards = resultIndex + targetRawIndex;
                var downwards = resultIndex + targetRawIndex - rawItemCount;
                var doubleDownwards = resultIndex + targetRawIndex - rawItemCount * 2;

                var firstPass = math.abs(downwards - currentIndex) > math.abs(upwards - currentIndex)
                    ? upwards
                    : downwards;
                var secondPass = math.abs(firstPass - currentIndex) > math.abs(doubleUpwards - currentIndex)
                    ? doubleUpwards
                    : firstPass;

                resultIndex = math.abs(secondPass - currentIndex) > math.abs(doubleDownwards - currentIndex)
                    ? doubleDownwards
                    : secondPass;
            }

            targetIndex = resultIndex;

            SnapToItem(resultIndex, scrollView, animated, true, byHand);
        }

        public class SnapToItemEventArgs : EventArgs
        {
            public bool IsByHand;
            public int TargetIndex { get; set; }
        }
    }
}