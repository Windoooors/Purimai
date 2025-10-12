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
    public static readonly List<Collider2D> SimultaneouslyTouchedSensorColliderList = new();

    [HideInInspector] public SimulatedSensorSettings settings;

    private bool _currentFrameHasFinger;
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

        foreach (var finger in Touch.activeFingers)
        {
            var rayPoint = _mainCamera.ScreenToWorldPoint(finger.screenPosition);

            var hits = Physics2D.RaycastAll(rayPoint, Vector2.zero);

            foreach (var hit in hits)
                if (hit && hit.collider.gameObject.name == settings.sensorId)
                    _currentFrameHasFinger = true;
        }

        if (_currentFrameHasFinger && !_lastFrameHadFinger)
        {
            OnTap?.Invoke(this, new TouchEventArgs(settings.sensorId));
            OnLeave += OnAnySensorLeave;

            SimultaneouslyTouchedSensorColliderList.Add(_sensorCollider);

            //_spriteShapeRenderer.color = new Color(1, 1, 1, 0.1f);
        }

        if (!_currentFrameHasFinger && _lastFrameHadFinger)
        {
            OnLeave?.Invoke(this, new TouchEventArgs(settings.sensorId));
            OnLeave -= OnAnySensorLeave;

            SimultaneouslyTouchedSensorColliderList.Remove(_sensorCollider);

            _notFirstFrameToHaveFingerHolding = false;

            //_spriteShapeRenderer.color = new Color(1, 1, 1, 0);
        }

        if (_currentFrameHasFinger && _lastFrameHadFinger)
            _notFirstFrameToHaveFingerHolding = true;

        _lastFrameHadFinger = _currentFrameHasFinger;
    }

    private void OnAnySensorLeave(object sender, TouchEventArgs e)
    {
        var overlapResults = new List<Collider2D>();

        _sensorCollider.Overlap(overlapResults);

        foreach (var simultaneouslyTouchedSensorCollider in SimultaneouslyTouchedSensorColliderList)
            if (overlapResults.Contains(simultaneouslyTouchedSensorCollider) &&
                e.SensorId == simultaneouslyTouchedSensorCollider.name)
                return;

        if ((_currentFrameHasFinger && _lastFrameHadFinger && _notFirstFrameToHaveFingerHolding) ||
            e.SensorId == settings.sensorId)
            OnHold?.Invoke(this, new TouchEventArgs(settings.sensorId));
    }
}