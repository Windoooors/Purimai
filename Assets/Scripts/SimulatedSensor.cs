using System;
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

    public class TouchEventArgs : EventArgs
    {
        public readonly string SensorId;

        public TouchEventArgs(string sensorId)
        {
            SensorId = sensorId;
        }
    }

    public string sensorId;

    private bool _lastFrameHadFinger;
    private bool _currentFrameHasFinger;
    private Camera _mainCamera;

    private void Start()
    {
        EnhancedTouchSupport.Enable();
        
        _mainCamera = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
        gameObject.name = sensorId;
        
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
            OnTap?.Invoke(this, new TouchEventArgs(sensorId));
        
        if (_currentFrameHasFinger && _lastFrameHadFinger)
            OnHold?.Invoke(this, new TouchEventArgs(sensorId));
        
        if (!_currentFrameHasFinger && _lastFrameHadFinger)
            OnLeave?.Invoke(this, new TouchEventArgs(sensorId));

        _lastFrameHadFinger = _currentFrameHasFinger;
    }
}
