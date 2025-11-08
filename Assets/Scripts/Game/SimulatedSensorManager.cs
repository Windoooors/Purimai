using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Game
{
    public class SimulatedSensorManager : MonoBehaviour
    {
        public static SimulatedSensorManager Instance;
        public float globalScale = 1.23f;
        public float offset;

        private void Awake()
        {
            Instance = this;
            EnhancedTouchSupport.Enable();

            if (FindObjectsByType<SimulatedSensorManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Length != 1)
                gameObject.SetActive(false);
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
}