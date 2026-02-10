using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class HoleMaskElement : VisualElement
    {
        [UxmlAttribute("Center")] private Vector2 _center = Vector2.zero;

        [UxmlAttribute("Radius")] private float _radius = 100f;

        public HoleMaskElement()
        {
            style.flexGrow = 1;
            pickingMode = PickingMode.Ignore;

            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var rect = contentRect;
            var center = _center == Vector2.zero ? rect.center : _center;
            var segments = 256;
            var maskColor = new Color(0, 0, 0, 1);

            var painter = mgc.Allocate((segments + 1) * 2, segments * 6);

            var outerRadius = Mathf.Max(rect.width, rect.height) * 1.5f;

            var vertices = new List<Vertex>();
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * 2 * Mathf.PI / segments;
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);

                vertices.Add(new Vertex
                {
                    position = new Vector3(center.x + cos * _radius, center.y + sin * _radius, Vertex.nearZ),
                    tint = maskColor
                });

                vertices.Add(new Vertex
                {
                    position = new Vector3(center.x + cos * outerRadius, center.y + sin * outerRadius, Vertex.nearZ),
                    tint = maskColor
                });
            }

            var indices = new List<ushort>();
            for (var i = 0; i < segments; i++)
            {
                var start = i * 2;
                indices.Add((ushort)start);
                indices.Add((ushort)(start + 1));
                indices.Add((ushort)(start + 2));

                indices.Add((ushort)(start + 1));
                indices.Add((ushort)(start + 3));
                indices.Add((ushort)(start + 2));
            }

            painter.SetAllVertices(vertices.ToArray());
            painter.SetAllIndices(indices.ToArray());
        }

        public void SetRadius(float r)
        {
            _radius = r;
            MarkDirtyRepaint();
        }
    }
}