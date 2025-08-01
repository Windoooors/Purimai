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

    private readonly Vector2 _pathPosition = Vector2.zero;

    private readonly Vector2 _pathScale = new(0.01005f, -0.01005f);

    private bool _isReturning;
    private bool _moving;

    private BezierPathSegment[] _path;
    private Vector2 _presetOffsetPosition;
    private float[] _segmentLengths;

    private Vector3 _startPosition;
    private float _time;
    private float _totalLength;

    public void Start()
    {
        var fullPath = Path.Combine(Application.streamingAssetsPath, "StarPath/" + svgAssetPath + ".svg");
        using var reader = new StreamReader(File.OpenRead(fullPath));

        var sceneInfo = SVGParser.ImportSVG(reader, ViewportOptions.DontPreserve);
        var shape = sceneInfo.NodeIDs.ToArray()[1].Value.Shapes[0];
        _path = shape.Contours[0].Segments.ToArray();

        var segmentCount = _path.Length - 1;
        _segmentLengths = new float[segmentCount];
        _totalLength = 0f;

        for (var i = 0; i < segmentCount; i++)
        {
            var p0 = _path[i].P0;
            var p1 = _path[i].P1;
            var p2 = _path[i].P2;
            var p3 = _path[i + 1].P0;
            var length = EstimateCubicLength(p0, p1, p2, p3, 10);
            _segmentLengths[i] = length;
            _totalLength += length;
        }

        _time = 0f;
    }

    private void Update()
    {
        if (!_moving || _path.Length < 2)
            return;

        //_flipPathY = transform.eulerAngles.y == 180f;

        var deltaTime = Time.deltaTime;
        _time += (_isReturning ? -1 : 1) * deltaTime;

        var t = Mathf.Clamp01(_time / duration);

        if (t >= 1f) _moving = false;

        Move(t);
    }

    private Matrix4x4 GetPathTransform()
    {
        var scale = new Vector3(flipPathY ? -_pathScale.x : _pathScale.x, _pathScale.y, 1);
        var position = _pathPosition;
        var rotation = Quaternion.Euler(0, 0, pathRotation);

        return Matrix4x4.TRS(position, rotation, scale);
    }

    private void Move(float progress)
    {
        var distance = progress * _totalLength;

        SamplePointAtDistance(distance, out var pos, out var tangent);

        var matrix = GetPathTransform();

        Vector2 worldPos = matrix.MultiplyPoint3x4(pos - _presetOffsetPosition);
        Vector2 worldTangent = matrix.MultiplyVector(tangent).normalized;

        transform.position = _startPosition + new Vector3(worldPos.x, worldPos.y, 0);
        var angle = Mathf.Atan2(worldTangent.y, worldTangent.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + objectRotationOffset);
    }

    public void MoveToStart()
    {
        _startPosition = transform.position;
        SamplePointAtDistance(0, out _presetOffsetPosition, out _);
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

    private void SamplePointAtDistance(float dist, out Vector2 position, out Vector2 tangent)
    {
        var accumulated = 0f;

        for (var i = 0; i < _segmentLengths.Length; i++)
        {
            if (accumulated + _segmentLengths[i] >= dist)
            {
                var localT = (dist - accumulated) / _segmentLengths[i];
                var p0 = _path[i].P0;
                var p1 = _path[i].P1;
                var p2 = _path[i].P2;
                var p3 = _path[i + 1].P0;

                position = EvaluateCubic(p0, p1, p2, p3, localT);
                tangent = EvaluateCubicTangent(p0, p1, p2, p3, localT).normalized;
                return;
            }

            accumulated += _segmentLengths[i];
        }

        var lp0 = _path[^2].P0;
        var lp1 = _path[^2].P1;
        var lp2 = _path[^2].P2;
        var lp3 = _path[^1].P0;

        position = lp3;
        tangent = EvaluateCubicTangent(lp0, lp1, lp2, lp3, 1f).normalized;
    }

    private Vector2 EvaluateCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        var u = 1 - t;
        return u * u * u * p0 +
               3 * u * u * t * p1 +
               3 * u * t * t * p2 +
               t * t * t * p3;
    }

    private Vector2 EvaluateCubicTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        var u = 1 - t;
        return
            3 * u * u * (p1 - p0) +
            6 * u * t * (p2 - p1) +
            3 * t * t * (p3 - p2);
    }

    private float EstimateCubicLength(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps)
    {
        var length = 0f;
        var previousPoint = p0;
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (float)steps;
            var point = EvaluateCubic(p0, p1, p2, p3, t);
            length += Vector2.Distance(previousPoint, point);
            previousPoint = point;
        }

        return length;
    }
}