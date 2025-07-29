using UnityEngine;

public class Lanes : MonoBehaviour
{
    public static Lanes Instance;

    public Transform[] startPoints;
    public Transform[] endPoints;

    private void Awake()
    {
        Instance = this;
    }
}