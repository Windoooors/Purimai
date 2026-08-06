using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class TouchPoint : MonoBehaviour
    {
        public static readonly List<TouchPoint> TouchPoints = new();
        public string sensorName;

        private void Awake()
        {
            TouchPoints.Add(this);
        }
    }
}