using System;
using System.Collections;
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

    private static bool _leaveEventRegistered;

    public float scale = 1;

    public string sensorId;

    private bool _currentFrameHasFinger;
    private bool _lastFrameHadFinger;

    private Camera _mainCamera;

    private SpriteShapeRenderer _spriteShapeRenderer;

    private void Start()
    {
        _mainCamera = SimulatedSensorManager.Instance.mainCamera;
        gameObject.name = sensorId;

        _spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();

        Sensors.Add(this);

        StartCoroutine(ChangeSensorScale());
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
            if (!_leaveEventRegistered)
            {
                OnLeave += OnAnySensorLeaved;
                _leaveEventRegistered = true;
            }
            else
            {
                StartCoroutine(WaitAndRegisterLeaveEvent());
            }

            OnTap?.Invoke(this, new TouchEventArgs(sensorId));

            _spriteShapeRenderer.color = new Color(1, 1, 1, 0.1f);
        }

        if (!_currentFrameHasFinger && _lastFrameHadFinger)
        {
            OnLeave?.Invoke(this, new TouchEventArgs(sensorId));
            OnLeave -= OnAnySensorLeaved;
            _leaveEventRegistered = false;

            _spriteShapeRenderer.color = new Color(1, 1, 1, 0);
        }

        _lastFrameHadFinger = _currentFrameHasFinger;
    }

    private IEnumerator WaitAndRegisterLeaveEvent()
    {
        yield return null;
        OnLeave += OnAnySensorLeaved;
        _leaveEventRegistered = true;
    }

    private IEnumerator ChangeSensorScale()
    {
        yield return null;
        transform.localScale *= scale * SimulatedSensorManager.Instance.globalScale;
    }

    private void OnAnySensorLeaved(object sender, TouchEventArgs e)
    {
        if ((_currentFrameHasFinger && _lastFrameHadFinger) || e.SensorId == sensorId)
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