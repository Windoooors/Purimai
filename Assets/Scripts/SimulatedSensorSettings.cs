using System.Collections;
using UnityEngine;

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
        transform.localScale *= scale * SimulatedSensorManager.Instance.globalScale;
    }
}