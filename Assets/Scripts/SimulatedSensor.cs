using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.U2D;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SimulatedSensor : MonoBehaviour
{
    public static EventHandler<TouchEventArgs> OnTap;
    public static EventHandler<TouchEventArgs> OnHold;
    public static EventHandler<TouchEventArgs> OnLeave;

    public static readonly List<SimulatedSensor> Sensors = new();

    public string sensorId;
    private bool _currentFrameHasFinger;

    private bool _lastFrameHadFinger;
    private Camera _mainCamera;

    private SpriteShapeRenderer _spriteShapeRenderer;

    private void Start()
    {
        EnhancedTouchSupport.Enable();

        _mainCamera = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
        gameObject.name = sensorId;

        _spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();

        Sensors.Add(this);
    }

    private void Update()
    {
        _currentFrameHasFinger = false;

        foreach (var finger in Touch.activeFingers)
        {
            var rayPoint = _mainCamera.ScreenToWorldPoint(finger.screenPosition);
            var hit = Physics2D.Raycast(rayPoint, Vector2.zero);

            if (hit && hit.collider.gameObject.name == sensorId)
                _currentFrameHasFinger = true;
        }

        if (_currentFrameHasFinger && !_lastFrameHadFinger)
        {
            OnTap += OnAnySensorTapped;
            OnTap?.Invoke(this, new TouchEventArgs(sensorId));
            _spriteShapeRenderer.color = new Color(1, 1, 1, 0.1f);
        }

        if (!_currentFrameHasFinger && _lastFrameHadFinger)
        {
            OnTap -= OnAnySensorTapped;
            OnLeave?.Invoke(this, new TouchEventArgs(sensorId));
            _spriteShapeRenderer.color = new Color(1, 1, 1, 0);
        }

        _lastFrameHadFinger = _currentFrameHasFinger;
    }

    private void OnAnySensorTapped(object sender, TouchEventArgs e)
    {
        if (_currentFrameHasFinger)
            OnHold?.Invoke(this, new TouchEventArgs(sensorId));
    }

    public class TouchEventArgs : EventArgs
    {
        public readonly string SensorId;

        public TouchEventArgs(string sensorId)
        {
            SensorId = sensorId;
        }
    }
}