#if UNITY_ANDROID
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VectorGraphics;
using UnityEngine;
using StringReader = System.IO.StringReader;

namespace Game
{
    public class VectorGraphicsUtility
    {
        private readonly bool _flipPathY;

        private readonly BezierPathSegment[] _path;

        private readonly Vector2 _pathPosition = Vector2.zero;
        private readonly float _pathRotation;

        private readonly Vector2 _pathScale = new(0.01005f, -0.01005f);
        private readonly int _samplesPerSegment = 10;
        private readonly float[][] _segmentCumLengths;
        private readonly float[] _segmentLengths;

        private List<(float, Quaternion _rotationBeforeTurningPoint, Quaternion _rotationAfterTurningPoint)>
            _turningPoints = new();

        // per-segment arc length tables
        private readonly float[][] _segmentSampleTs;
        private readonly float _totalLength;
        private Vector2 _presetOffsetPosition;

        private Vector3 _startPosition;

        public float ObjectRotationOffset;

        public VectorGraphicsUtility(string svgAssetPath, float pathRotation, bool flipPathY, Vector3 startPosition,
            float objectRotationOffset = 18f)
        {
            _pathRotation = pathRotation;
            _flipPathY = flipPathY;
            ObjectRotationOffset = objectRotationOffset;

            using var reader = new StringReader(GetSvgText("star_path/" + svgAssetPath + ".svg"));

            var sceneInfo = SVGParser.ImportSVG(reader, ViewportOptions.DontPreserve);
            var shape = sceneInfo.NodeIDs.ToArray()[1].Value.Shapes[0];
            _path = shape.Contours[0].Segments.ToArray();

            var segmentCount = _path.Length - 1;
            _segmentSampleTs = new float[segmentCount][];
            _segmentCumLengths = new float[segmentCount][];
            _segmentLengths = new float[segmentCount];

            _totalLength = 0f;

            for (var i = 0; i < segmentCount; i++)
            {
                var p0 = _path[i].P0;
                var p1 = _path[i].P1;
                var p2 = _path[i].P2;
                var p3 = _path[i + 1].P0;

                var n = _samplesPerSegment;
                var sampleTs = new float[n + 1];
                var cumulativeLengths = new float[n + 1];
                sampleTs[0] = 0f;
                cumulativeLengths[0] = 0f;

                var prev = EvaluateCubic(p0, p1, p2, p3, 0f);
                var acc = 0f;

                for (var s = 1; s <= n; s++)
                {
                    var t = s / (float)n;
                    sampleTs[s] = t;
                    var pt = EvaluateCubic(p0, p1, p2, p3, t);
                    acc += Vector2.Distance(prev, pt);
                    cumulativeLengths[s] = acc;
                    prev = pt;
                }

                _segmentSampleTs[i] = sampleTs;
                _segmentCumLengths[i] = cumulativeLengths;
                _segmentLengths[i] = cumulativeLengths[n]; // total length of this segment
                _totalLength += _segmentLengths[i];
            }
        }

        public void FindTurningPoints()
        {
            var step = 0.01f;
            var lastRotation = GetPositionRotationPair(0, false).rotation;

            for (var current = 0.1f; current <= 1; current += step)
            {
                var rotation = GetPositionRotationPair(current, false).rotation;

                var deltaRotation = Quaternion.Angle(rotation, lastRotation);
                
                if (deltaRotation > 20)
                {
                    var turningPoint = current;
                    
                    var rotationBeforeTurningPoint = GetPositionRotationPair(turningPoint - 0.05f, false).rotation;
                    var rotationAfterTurningPoint = GetPositionRotationPair(turningPoint + 0.05f, false).rotation;

                    _turningPoints.Add((turningPoint, rotationBeforeTurningPoint, rotationAfterTurningPoint));
                }
                
                lastRotation = rotation;
            }
        }
        
        public void SetStartPosition(Vector3 startPosition)
        {
            _startPosition = startPosition;
            SamplePointAtDistance(0, out _presetOffsetPosition, out _);
        }

        private Matrix4x4 GetPathTransform()
        {
            var scale = new Vector3(_flipPathY ? -_pathScale.x : _pathScale.x, _pathScale.y, 1);
            var position = _pathPosition;
            var rotation = Quaternion.Euler(0, 0, _pathRotation);

            return Matrix4x4.TRS(position, rotation, scale);
        }

        public (Vector3 position, Quaternion rotation) GetPositionRotationPair(float progress, bool isStar)
        {
            var distance = progress * _totalLength;

            SamplePointAtDistance(distance, out var pos, out var tangent);

            var matrix = GetPathTransform();

            Vector2 worldPos = matrix.MultiplyPoint3x4(pos - _presetOffsetPosition);
            Vector2 worldTangent = matrix.MultiplyVector(tangent).normalized;
            
            if (!isStar || _turningPoints.Count == 0 || !InTurningProgress(out var turningPoint))
            {
                var angle = Mathf.Atan2(worldTangent.y, worldTangent.x) * Mathf.Rad2Deg;

                return (_startPosition + new Vector3(worldPos.x, worldPos.y, 0),
                    Quaternion.Euler(0f, 0f, angle + ObjectRotationOffset));
            }
            
            var rotation = Quaternion.Lerp(turningPoint?.before ?? new Quaternion(), turningPoint?.after ?? new Quaternion(),
                (progress - (turningPoint?.Item1 ?? 0) + 0.01f) / 0.05f);

            return (_startPosition + new Vector3(worldPos.x, worldPos.y, 0),
                rotation);

            bool InTurningProgress(out (float, Quaternion before, Quaternion after)? turningPoint)
            {
                foreach (var point in _turningPoints)
                {
                    if (progress >= point.Item1 - 0.01f && progress <= point.Item1 + 0.04f)
                    {
                        turningPoint = point;

                        return true;
                    }
                }

                turningPoint = null;
                return false;
            }
        }

        private void SamplePointAtDistance(float dist, out Vector2 position, out Vector2 tangent)
        {
            // clamp
            if (dist <= 0f)
            {
                var lp0 = _path[0].P0;
                var lp1 = _path[0].P1;
                var lp2 = _path[0].P2;
                var lp3 = _path[1].P0;
                position = EvaluateCubic(lp0, lp1, lp2, lp3, 0f);
                tangent = EvaluateCubicTangent(lp0, lp1, lp2, lp3, 0f).normalized;
                return;
            }

            if (dist >= _totalLength)
            {
                var lp0 = _path[^2].P0;
                var lp1 = _path[^2].P1;
                var lp2 = _path[^2].P2;
                var lp3 = _path[^1].P0;
                position = EvaluateCubic(lp0, lp1, lp2, lp3, 1f);
                tangent = EvaluateCubicTangent(lp0, lp1, lp2, lp3, 1f).normalized;
                return;
            }

            var accumulated = 0f;
            for (var i = 0; i < _segmentLengths.Length; i++)
            {
                var segLen = _segmentLengths[i];
                if (accumulated + segLen >= dist)
                {
                    var localDist = dist - accumulated;
                    // find t inside this segment using its cumLengths table
                    var cum = _segmentCumLengths[i];
                    var ts = _segmentSampleTs[i];
                    var idx = Array.BinarySearch(cum, localDist);
                    float t;
                    if (idx >= 0)
                    {
                        t = ts[idx];
                    }
                    else
                    {
                        var insert = ~idx;
                        // localDist is between cum[insert-1] and cum[insert]
                        var a = Mathf.Clamp(insert - 1, 0, cum.Length - 1);
                        var b = Mathf.Clamp(insert, 0, cum.Length - 1);
                        if (a == b)
                        {
                            t = ts[a];
                        }
                        else
                        {
                            var la = cum[a];
                            var lb = cum[b];
                            var fa = (localDist - la) / (lb - la);
                            t = Mathf.Lerp(ts[a], ts[b], fa);
                        }
                    }

                    var p0 = _path[i].P0;
                    var p1 = _path[i].P1;
                    var p2 = _path[i].P2;
                    var p3 = _path[i + 1].P0;
                    position = EvaluateCubic(p0, p1, p2, p3, t);
                    tangent = EvaluateCubicTangent(p0, p1, p2, p3, t).normalized;
                    return;
                }

                accumulated += segLen;
            }

            // fallback (shouldn't reach)
            var last0 = _path[^2].P0;
            var last1 = _path[^2].P1;
            var last2 = _path[^2].P2;
            var last3 = _path[^1].P0;
            position = EvaluateCubic(last0, last1, last2, last3, 1f);
            tangent = EvaluateCubicTangent(last0, last1, last2, last3, 1f).normalized;
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

        private string GetSvgText(string path)
        {
            return BetterStreamingAssets.ReadAllText(path);
        }
    }
}