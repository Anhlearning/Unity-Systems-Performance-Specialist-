// Procedural meshes for the stage: the core each instance wears, keyed by Vòng
// 2's pattern (A07), plus the ground quad, the glow quad and the floor grid.
// Every core faces its local +Z, so a LookRotation points it along the heading.
// All meshes carry white vertex colors because the unlit shader multiplies by them.

using System.Collections.Generic;
using MagicCircleSim.Design;
using UnityEngine;

namespace MagicCircleSim.View
{
    public static class StageMeshes
    {
        /// <summary>Instance core size, meters, before the look's own scale.</summary>
        const float CoreM = 0.55f;

        public static Mesh Core(PatternId pattern)
        {
            switch (pattern)
            {
                case PatternId.Nhon: return Cone(CoreM * 0.45f, CoreM * 1.6f, 12);
                case PatternId.Vuong: return Box(CoreM);
                default: return Sphere(CoreM * 0.55f, 20, 14);
            }
        }

        /// <summary>A cone centered on the origin with its tip at +Z.</summary>
        static Mesh Cone(float radius, float height, int segments)
        {
            var vertices = new List<Vector3> { new Vector3(0, 0, height / 2), new Vector3(0, 0, -height / 2) };
            var triangles = new List<int>();
            for (var i = 0; i < segments; i++)
            {
                var a = i * Mathf.PI * 2 / segments;
                vertices.Add(new Vector3(radius * Mathf.Cos(a), radius * Mathf.Sin(a), -height / 2));
            }
            for (var i = 0; i < segments; i++)
            {
                int current = 2 + i, next = 2 + (i + 1) % segments;
                triangles.AddRange(new[] { 0, next, current, 1, current, next });
            }
            return Build("Core Cone", vertices, triangles);
        }

        static Mesh Box(float side)
        {
            var h = side / 2;
            var vertices = new List<Vector3>();
            for (var i = 0; i < 8; i++) vertices.Add(new Vector3((i & 1) == 0 ? -h : h, (i & 2) == 0 ? -h : h, (i & 4) == 0 ? -h : h));
            var triangles = new List<int>
            {
                0, 2, 1, 1, 2, 3, 4, 5, 6, 5, 7, 6,
                0, 1, 4, 1, 5, 4, 2, 6, 3, 3, 6, 7,
                0, 4, 2, 2, 4, 6, 1, 3, 5, 3, 7, 5,
            };
            return Build("Core Box", vertices, triangles);
        }

        static Mesh Sphere(float radius, int longitude, int latitude)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var lat = 0; lat <= latitude; lat++)
            {
                var theta = lat * Mathf.PI / latitude;
                for (var lon = 0; lon <= longitude; lon++)
                {
                    var phi = lon * Mathf.PI * 2 / longitude;
                    vertices.Add(radius * new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi)));
                }
            }
            for (var lat = 0; lat < latitude; lat++)
            {
                for (var lon = 0; lon < longitude; lon++)
                {
                    int a = lat * (longitude + 1) + lon, b = a + longitude + 1;
                    triangles.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
                }
            }
            return Build("Core Sphere", vertices, triangles);
        }

        /// <summary>A quad on the XZ plane. UV v grows toward +Z, so a texture's top row faces forward.</summary>
        public static Mesh GroundQuad(float size)
        {
            var h = size / 2;
            var mesh = Build("Ground Quad",
                new List<Vector3> { new Vector3(-h, 0, -h), new Vector3(h, 0, -h), new Vector3(-h, 0, h), new Vector3(h, 0, h) },
                new List<int> { 0, 2, 1, 1, 2, 3 });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            return mesh;
        }

        /// <summary>A unit quad on the XY plane, for camera-facing glows.</summary>
        public static Mesh BillboardQuad()
        {
            var mesh = Build("Glow Quad",
                new List<Vector3> { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) },
                new List<int> { 0, 2, 1, 1, 2, 3 });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            return mesh;
        }

        /// <summary>three.js's GridHelper: <paramref name="divisions"/> cells across, center lines brighter.</summary>
        public static Mesh Grid(float size, int divisions, Color center, Color line)
        {
            // Vertex colors skip the sRGB conversion material colors get, so convert them here.
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                center = center.linear;
                line = line.linear;
            }
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();
            var h = size / 2;
            var step = size / divisions;
            for (var i = 0; i <= divisions; i++)
            {
                var k = -h + i * step;
                var color = i == divisions / 2 ? center : line;
                foreach (var p in new[] { new Vector3(-h, 0, k), new Vector3(h, 0, k), new Vector3(k, 0, -h), new Vector3(k, 0, h) })
                {
                    indices.Add(vertices.Count);
                    vertices.Add(p);
                    colors.Add(color);
                }
            }
            var mesh = new Mesh { name = "Floor Grid" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            return mesh;
        }

        static Mesh Build(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            var white = new List<Color32>(vertices.Count);
            for (var i = 0; i < vertices.Count; i++) white.Add(new Color32(255, 255, 255, 255));
            mesh.SetColors(white);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
