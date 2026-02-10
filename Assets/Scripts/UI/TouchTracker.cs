using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace UI
{
    public class TouchTracker : IDisposable
    {
        private const float MoveThreshold = 1f;
        private readonly Action _onPressed;
        private readonly Action _onReleased;
        private readonly VisualElement _target;

        private Finger _activeFinger;
        private bool _hasTriggeredPressed;
        private Vector2 _startPos;

        public TouchTracker(VisualElement target, Action onPressed, Action onReleased)
        {
            _target = target;
            _onPressed = onPressed;
            _onReleased = onReleased;

            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();

            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerMove += OnFingerMove;
            Touch.onFingerUp += OnFingerUp;
        }

        public void Dispose()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerMove -= OnFingerMove;
            Touch.onFingerUp -= OnFingerUp;
        }

        private void OnFingerDown(Finger finger)
        {
            if (_activeFinger != null) return;

            var screenPos = finger.currentTouch.screenPosition;

            if (IsPointerOverElement(screenPos))
            {
                _activeFinger = finger;
                _startPos = screenPos;
                _hasTriggeredPressed = false;
            }
        }

        private void OnFingerMove(Finger finger)
        {
            if (_activeFinger != finger || _hasTriggeredPressed) return;

            var dist = Vector2.Distance(finger.currentTouch.screenPosition, _startPos);

            if (dist > MoveThreshold)
            {
                _hasTriggeredPressed = true;
                _onPressed?.Invoke();
            }
        }

        private void OnFingerUp(Finger finger)
        {
            if (_activeFinger == finger)
            {
                _activeFinger = null;

                _onReleased?.Invoke();
            }
        }

        private bool IsPointerOverElement(Vector2 screenPos)
        {
            if (_target == null || _target.panel == null) return false;

            var root = _target.panel.visualTree;
            var panelPos = RuntimePanelUtils.ScreenToPanel(_target.panel, screenPos);

            var correctedY = root.layout.height - panelPos.y;
            var finalPos = new Vector2(panelPos.x, correctedY);

            var pickedElement = _target.panel.Pick(finalPos);

            if (pickedElement == null) return false;

            var isHit = _target == pickedElement || _target.Contains(pickedElement);

            return isHit;
        }
    }
}