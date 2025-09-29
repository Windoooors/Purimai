using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SimulatedSensorAllowingOverlapping : MonoBehaviour
{
    public static EventHandler<TouchEventArgs> OnTap;
    public static EventHandler<TouchEventArgs> OnTapDelayed;
    public static EventHandler<TouchEventArgs> OnHold;
    public static EventHandler<TouchEventArgs> OnHoldUpdate;
    public static EventHandler<TouchEventArgs> OnLeave;

    public static readonly List<SimulatedSensorAllowingOverlapping> Sensors = new();

    private bool _currentFrameHasFinger;
    private bool _lastFrameHadFinger;
    private bool _fingerTapped;

    private Camera _mainCamera;

    private SpriteShapeRenderer _spriteShapeRenderer;

    private bool _notFirstFrameToHaveFingerHolding;

    [HideInInspector] public SimulatedSensorSettings settings;

    private void Start()
    {
        settings = GetComponent<SimulatedSensorSettings>();
        _mainCamera = SimulatedSensorManager.Instance.mainCamera;

        _spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();

        Sensors.Add(this);
    }

    private void Update()
    {
        _currentFrameHasFinger = false;

        foreach (var finger in Touch.activeFingers)
        {
            var rayPoint = _mainCamera.ScreenToWorldPoint(finger.screenPosition);

            var hits = Physics2D.RaycastAll(rayPoint, Vector2.zero);

            foreach (var hit in hits)
                if (hit && hit.collider.gameObject.name == settings.sensorId)
                    _currentFrameHasFinger = true;
        }
        
        if (_currentFrameHasFinger && _lastFrameHadFinger && _fingerTapped)
        {
            _fingerTapped = false;
            
            OnTapDelayed?.Invoke(this, new TouchEventArgs(settings.sensorId, false));
        }

        if (_currentFrameHasFinger && !_lastFrameHadFinger)
        {
            OnTap?.Invoke(this, new TouchEventArgs(settings.sensorId, true));
            OnLeave += OnAnySensorLeave;

            _fingerTapped = true;

            _spriteShapeRenderer.color = new Color(1, 1, 1, 0.1f);
        }
        
        if (_currentFrameHasFinger && _lastFrameHadFinger)
            OnHoldUpdate?.Invoke(this, new TouchEventArgs(settings.sensorId, true));

        if (!_currentFrameHasFinger && _lastFrameHadFinger)
        {
            OnLeave?.Invoke(this, new TouchEventArgs(settings.sensorId, true));
            OnLeave -= OnAnySensorLeave;

            _notFirstFrameToHaveFingerHolding = false;

            _spriteShapeRenderer.color = new Color(1, 1, 1, 0);
        }

        if (_currentFrameHasFinger && _lastFrameHadFinger)
            _notFirstFrameToHaveFingerHolding = true;

        _lastFrameHadFinger = _currentFrameHasFinger;
    }

    private void OnAnySensorLeave(object sender, TouchEventArgs e)
    {
        if (_currentFrameHasFinger && _lastFrameHadFinger && _notFirstFrameToHaveFingerHolding ||
            e.SensorId == settings.sensorId)
            OnHold?.Invoke(this, new TouchEventArgs(settings.sensorId, true));
    }
}