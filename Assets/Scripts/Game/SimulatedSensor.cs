using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Game
{
    public class SimulatedSensor : MonoBehaviour
    {
        public static EventHandler<TouchEventArgs> OnTap;
        public static EventHandler<TouchEventArgs> OnHold;
        public static EventHandler<TouchEventArgs> OnLeave;

        public static bool Enabled = true;

        public static readonly List<SimulatedSensor> Sensors = new();

        [HideInInspector] public SimulatedSensorSettings settings;
        private readonly List<Finger> _activeFingers = new();

        private Collider2D _collider;
        private Finger _currentFinger;
        private Camera _mainCamera;

        private void Start()
        {
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerMove += OnFingerMove;
            Touch.onFingerUp += OnFingerUp;

            _collider = GetComponent<Collider2D>();
            settings = GetComponent<SimulatedSensorSettings>();
            _mainCamera = Camera.main;

            Sensors.Add(this);

            SceneManager.sceneLoaded += (_, _) => _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (_activeFingers.Count > 0)
                if (Enabled)
                    OnHold?.Invoke(this,
                        new TouchEventArgs(settings.sensorId));
        }

        private void OnDestroy()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerMove -= OnFingerMove;
            Touch.onFingerUp -= OnFingerUp;

            Sensors.Clear();
        }

        public static void Clear()
        {
            OnTap = null;
            OnHold = null;
            OnLeave = null;
        }
        
        private void OnFingerDown(Finger finger)
        {
            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(finger.screenPosition);
            if (!_collider.OverlapPoint(worldPos) || _activeFingers.Contains(finger))
                return;

            _activeFingers.Add(finger);
            if (Enabled)
                OnTap?.Invoke(this, new TouchEventArgs(settings.sensorId));
        }

        private void OnFingerUp(Finger finger)
        {
            if (!_activeFingers.Contains(finger)) return;

            _activeFingers.Remove(finger);

            if (_activeFingers.Count == 0)
                if (Enabled)
                    OnLeave?.Invoke(this, new TouchEventArgs(settings.sensorId));
        }

        private void OnFingerMove(Finger finger)
        {
            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(finger.screenPosition);
            var isInside = _collider.OverlapPoint(worldPos);
            var isTracked = _activeFingers.Contains(finger);

            if (isInside && !isTracked)
            {
                _activeFingers.Add(finger);
                if (Enabled) OnTap?.Invoke(this, new TouchEventArgs(settings.sensorId));
            }
            else if (!isInside && isTracked)
            {
                _activeFingers.Remove(finger);
                if (_activeFingers.Count == 0)
                    if (Enabled)
                        OnLeave?.Invoke(this, new TouchEventArgs(settings.sensorId));
            }
        }
    }
}