using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SimulatedSensor : MonoBehaviour
{
    public static EventHandler<TouchEventArgs> OnTap;
    public static EventHandler<TouchEventArgs> OnHold;
    public static EventHandler<TouchEventArgs> OnLeave;

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
    }

    private void Update()
    {
        if (_activeFingers.Count > 0)
            StartCoroutine(TriggerEvent(() => { OnHold?.Invoke(this, new TouchEventArgs(settings.sensorId)); }));
    }


    private void OnFingerDown(Finger finger)
    {
        Vector2 worldPos = _mainCamera.ScreenToWorldPoint(finger.screenPosition);
        if (!_collider.OverlapPoint(worldPos) || _activeFingers.Contains(finger))
            return;

        _activeFingers.Add(finger);
        StartCoroutine(TriggerEvent(() => { OnTap?.Invoke(this, new TouchEventArgs(settings.sensorId)); }));
    }

    private void OnFingerUp(Finger finger)
    {
        if (!_activeFingers.Contains(finger)) return;

        _activeFingers.Remove(finger);

        if (_activeFingers.Count == 0)
            StartCoroutine(TriggerEvent(() => { OnLeave?.Invoke(this, new TouchEventArgs(settings.sensorId)); }));
    }

    private void OnFingerMove(Finger finger)
    {
        Vector2 worldPos = _mainCamera.ScreenToWorldPoint(finger.screenPosition);
        var isInside = _collider.OverlapPoint(worldPos);
        var isTracked = _activeFingers.Contains(finger);

        if (isInside && !isTracked)
        {
            _activeFingers.Add(finger);
            StartCoroutine(TriggerEvent(() => { OnTap?.Invoke(this, new TouchEventArgs(settings.sensorId)); }));
        }
        else if (!isInside && isTracked)
        {
            _activeFingers.Remove(finger);
            if (_activeFingers.Count == 0)
                StartCoroutine(TriggerEvent(() => { OnLeave?.Invoke(this, new TouchEventArgs(settings.sensorId)); }));
        }
    }

    private IEnumerator TriggerEvent(Action callback)
    {
        yield return new WaitForSeconds(SimulatedSensorManager.Instance.offset);

        callback?.Invoke();
    }
}