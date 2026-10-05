// A body's strokes drawn as camera-facing ribbons: each polyline point becomes
// a pair of vertices spread across the line of sight, so a stroke reads as a
// painted band from any angle. One mesh for the whole body, rebuilt each frame
// into reused lists, so a frame allocates nothing once the lists have grown.

using System;
using System.Collections.Generic;
using MagicCircleSim.Cast;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicCircleSim.View
{
    public sealed class StrokeRenderer : IDisposable
    {
        /// <summary>Generous bounds so the body is never culled while it sways or travels.</summary>
        const float BoundsM = 200;

        readonly Mesh mesh;
        readonly GameObject go;
        readonly Transform stage;
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<int> indices = new List<int>();
        readonly MaterialPropertyBlock props = new MaterialPropertyBlock();

        public StrokeRenderer(Transform parent, Transform stage, Material material, Texture texture, Color color)
        {
            this.stage = stage;
            mesh = new Mesh { name = "Body Strokes", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            go = ViewUtil.Child(parent, "Body");
            var renderer = ViewUtil.Renderer(go, mesh, material);
            props.SetTexture(StageAssets.MainTexId, texture);
            props.SetColor(StageAssets.ColorId, color);
            renderer.SetPropertyBlock(props);
        }

        public void Render(StrokeBuffer strokes, Camera viewer)
        {
            vertices.Clear();
            uvs.Clear();
            colors.Clear();
            indices.Clear();
            var eye = viewer != null ? stage.InverseTransformPoint(viewer.transform.position) : new Vector3(0, 10, -15);
            var p = strokes.Points;
            for (var s = 0; s < strokes.StrokeCount; s++)
            {
                var start = strokes.Starts[s];
                var count = strokes.Counts[s];
                var tone = (byte)Mathf.RoundToInt(Mathf.Clamp01(strokes.Tones[s]) * 255);
                var first = vertices.Count;
                for (var i = 0; i < count; i++)
                {
                    var k = start + i;
                    var here = new Vector3(p[k * 3], p[k * 3 + 1], p[k * 3 + 2]);
                    var prev = Math.Max(i - 1, 0) + start;
                    var next = Math.Min(i + 1, count - 1) + start;
                    var tangent = new Vector3(p[next * 3] - p[prev * 3], p[next * 3 + 1] - p[prev * 3 + 1], p[next * 3 + 2] - p[prev * 3 + 2]);
                    var side = Vector3.Cross(tangent, eye - here);
                    var length = side.magnitude;
                    side = length > 1e-6f ? side / length * (strokes.Widths[k] * 0.5f) : Vector3.zero;
                    vertices.Add(here - side);
                    vertices.Add(here + side);
                    var u = count > 1 ? (float)i / (count - 1) : 0;
                    uvs.Add(new Vector2(u, 0));
                    uvs.Add(new Vector2(u, 1));
                    var alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(strokes.Alphas[k]) * 255);
                    var color = new Color32(tone, tone, tone, alpha);
                    colors.Add(color);
                    colors.Add(color);
                }
                for (var i = 0; i < count - 1; i++)
                {
                    var a = first + i * 2;
                    indices.Add(a);
                    indices.Add(a + 2);
                    indices.Add(a + 1);
                    indices.Add(a + 1);
                    indices.Add(a + 2);
                    indices.Add(a + 3);
                }
            }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * BoundsM);
        }

        public void Dispose()
        {
            ViewUtil.Destroy(go);
            ViewUtil.Destroy(mesh);
        }
    }
}
