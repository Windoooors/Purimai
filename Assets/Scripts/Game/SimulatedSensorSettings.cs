using System.Collections;
using UI.GameSettings;
using UnityEngine;

namespace Game
{
    public class SimulatedSensorSettings : MonoBehaviour
    {
        public float scale = 1;

        public string sensorId;

        private void Start()
        {
            gameObject.name = sensorId;

            StartCoroutine(ChangeSensorScale());
        }

        private IEnumerator ChangeSensorScale()
        {
            yield return null;

           var settingsValue =  SettingsPool.GetValue("game.sensor_radius");

           transform.localScale *= scale * (settingsValue / 10f + 1f);
        }
    }
}