using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SimulatedSensor : MonoBehaviour
{
    public static EventHandler<TouchEventArgs> OnTap;
    public static EventHandler<TouchEventArgs> OnHold;
    public static EventHandler<TouchEventArgs> OnLeave;

    public static readonly List<SimulatedSensor> Sensors = new();

    [HideInInspector] public SimulatedSensorSettings settings;

    private bool _currentFrameHasFinger;

    private int _fingerCount;

    private int _lastFrameFingerCount;
    private bool _lastFrameHadFinger;

    private Camera _mainCamera;

    private bool _notFirstFrameToHaveFingerHolding;
    private Collider2D _sensorCollider;

    private SpriteShapeRenderer _spriteShapeRenderer;

    private void Start()
    {
        settings = GetComponent<SimulatedSensorSettings>();

        _mainCamera = SimulatedSensorManager.Instance.mainCamera;

        _spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();

        Sensors.Add(this);

        _sensorCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        _currentFrameHasFinger = false;

        _fingerCount = 0;

        foreach (var finger in Touch.activeFingers)
        {
            var rayPoint = _mainCamera.ScreenToWorldPoint(finger.screenPosition);

            var hits = Physics2D.RaycastAll(rayPoint, Vector2.zero);

            foreach (var hit in hits)
                if (hit && hit.collider.gameObject.name == settings.sensorId)
                {
                    _currentFrameHasFinger = true;
                    _fingerCount++;
                }
        }

        if (_currentFrameHasFinger && !_lastFrameHadFinger)
        {
            if (_lastFrameFingerCount - _fingerCount < 0)
                for (var i = 0; i < _fingerCount - _lastFrameFingerCount; i++)
                    OnTap?.Invoke(this, new TouchEventArgs(settings.sensorId));

            OnHold?.Invoke(this, new TouchEventArgs(settings.sensorId));

            //_spriteShapeRenderer.color = new Color(1, 1, 1, 0.1f);
        }

        if (!_currentFrameHasFinger && _lastFrameHadFinger)
        {
            OnLeave?.Invoke(this, new TouchEventArgs(settings.sensorId));
            //_notFirstFrameToHaveFingerHolding = false;
            //_spriteShapeRenderer.color = new Color(1, 1, 1, 0);
        }

        if (_currentFrameHasFinger && _lastFrameHadFinger)
            //_notFirstFrameToHaveFingerHolding = true;
            OnHold?.Invoke(this, new TouchEventArgs(settings.sensorId));

        _lastFrameHadFinger = _currentFrameHasFinger;
        _lastFrameFingerCount = _fingerCount;
    }
}