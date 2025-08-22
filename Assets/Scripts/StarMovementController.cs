using System.IO;
using System.Linq;
using Unity.VectorGraphics;
using UnityEngine;

public class StarMovementController : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    public string svgAssetPath;

    [HideInInspector] public float duration = 5f;

    [HideInInspector] public float pathRotation;

    [HideInInspector] public bool flipPathY;

    [HideInInspector] public float objectRotationOffset = -18;

    private bool _isReturning;
    private bool _moving;
    
    private float _time;
    
    private VectorGraphicsUtility _vectorGraphicsUtility;

    public void Start()
    {
        _vectorGraphicsUtility = new VectorGraphicsUtility(svgAssetPath, pathRotation, flipPathY, transform.position, objectRotationOffset);
    }

    private void Update()
    {
        if (!_moving)
            return;

        var deltaTime = Time.deltaTime;
        _time += (_isReturning ? -1 : 1) * deltaTime;

        var t = Mathf.Clamp01(_time / duration);

        if (t >= 1f) _moving = false;

        Move(t);
    }

    private void Move(float progress)
    {
        var nextPositionRotationPair = _vectorGraphicsUtility.GetPositionRotationPair(progress);

        transform.position = nextPositionRotationPair.position;
        transform.rotation = nextPositionRotationPair.rotation;
    }

    public void MoveToStart()
    {
        _vectorGraphicsUtility.SetStartPosition(transform.position);
        Move(0.001f); // 0.001f is for fixing some bizarre start point rotation issues.
        _time = duration * 0.001f;
    }

    public void StartMoving()
    {
        _moving = true;
    }

    public void StopMoving()
    {
        _moving = false;
    }
}