using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class SimulatedSensorManager : MonoBehaviour
{
    public static SimulatedSensorManager Instance;
    public float globalScale = 1.23f;
    public Camera mainCamera;

    private void Awake()
    {
        mainCamera = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
        Instance = this;
        EnhancedTouchSupport.Enable();
    }
}

public class TouchEventArgs : EventArgs
{
    public readonly string SensorId;

    public TouchEventArgs(string sensorId)
    {
        SensorId = sensorId;
    }
}