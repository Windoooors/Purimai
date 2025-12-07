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

            ChangeSensorScale();

            SettingsController.OnSettingsChanged += (_, _) => ChangeSensorScale();
        }

        private void ChangeSensorScale()
        {
            var globalSettingsValue = SettingsPool.GetValue("game.sensor_radius");
            var settingsValue = SettingsPool.GetValue(scaleSettingsIdentifier);

            transform.localScale = scale * (globalSettingsValue / 10f + 1f) * (settingsValue / 10f + 1f) * Vector3.one;
        }
    }
}