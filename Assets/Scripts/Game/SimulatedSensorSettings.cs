using System.Collections;
using UI.GameSettings;
using UnityEngine;

namespace Game
{
    public class SimulatedSensorSettings : MonoBehaviour
    {
        public float scale = 1;

        public string sensorId;

        public string scaleSettingsIdentifier;

        private void Start()
        {
            gameObject.name = sensorId;

            StartCoroutine(ChangeSensorScale());
        }

        private IEnumerator ChangeSensorScale()
        {
            yield return null;

            var globalSettingsValue = SettingsPool.GetValue("game.sensor_radius");
            var settingsValue = SettingsPool.GetValue(scaleSettingsIdentifier);

            transform.localScale *= scale * (globalSettingsValue / 10f + 1f) * (settingsValue / 10f + 1f);
        }
    }
}