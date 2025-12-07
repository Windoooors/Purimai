using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class SensorShape : MonoBehaviour
    {
        public static readonly List<SensorShape> SensorShapes = new();

        public string sensorId;

        private void Awake()
        {
            SensorShapes.Add(this);
        }
    }
}