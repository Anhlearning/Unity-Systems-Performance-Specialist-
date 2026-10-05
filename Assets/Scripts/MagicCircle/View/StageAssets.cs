// The shared GPU resources every cast draws with: one material per blend mode,
// the soft glow texture, and the color parsing the look table needs. Colors per
// instance go through MaterialPropertyBlocks, so no material is ever cloned.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MagicCircleSim.View
{
    public sealed class StageAssets : IDisposable
    {
        public static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

        const int GlowTexturePx = 128;

        public readonly Material Additive;
        public readonly Material AlphaBlend;
        /// <summary>Alpha blend in the opaque queue, so the floor always draws before the additive circle.</summary>
        public readonly Material Floor;
        public readonly Material Glow;
        public readonly Material Particles;
        public readonly Texture2D GlowTexture;
        readonly List<Object> owned = new List<Object>();

        public StageAssets(Shader unlit, Shader particles)
        {
            if (unlit == null || particles == null) throw new ArgumentException("CastStage needs both MagicCircle shaders assigned.");
            Additive = Unlit(unlit, BlendMode.SrcAlpha, BlendMode.One, "MagicCircle Additive");
            AlphaBlend = Unlit(unlit, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, "MagicCircle Alpha");
            Floor = Unlit(unlit, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, "MagicCircle Floor");
            Floor.renderQueue = (int)RenderQueue.Geometry;
            GlowTexture = SoftDot(GlowTexturePx);
            owned.Add(GlowTexture);
            Glow = Unlit(unlit, BlendMode.SrcAlpha, BlendMode.One, "MagicCircle Glow");
            Glow.SetTexture(MainTexId, GlowTexture);
            Particles = Own(new Material(particles) { name = "MagicCircle Particles" });
        }

        Material Unlit(Shader shader, BlendMode src, BlendMode dst, string name)
        {
            var material = Own(new Material(shader) { name = name });
            material.SetFloat(SrcBlendId, (float)src);
            material.SetFloat(DstBlendId, (float)dst);
            return material;
        }

        public Material Own(Material material)
        {
            owned.Add(material);
            return material;
        }

        /// <summary>The web build's radial gradient: white, alpha 1 at the center, 0.35 at 35%, 0 at the rim.</summary>
        static Texture2D SoftDot(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "MagicCircle Glow", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var r = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude / (size / 2f);
                    var a = r < 0.35f ? Mathf.Lerp(1f, 0.35f, r / 0.35f) : Mathf.Lerp(0.35f, 0f, (r - 0.35f) / 0.65f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        public static Color ParseColor(string hex, float alpha = 1)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out var color)) throw new FormatException($"Not a color: {hex}");
            color.a = alpha;
            return color;
        }

        public void Dispose()
        {
            foreach (var item in owned) ViewUtil.Destroy(item);
            owned.Clear();
        }
    }

    static class ViewUtil
    {
        /// <summary>Destroy that also works in edit mode, where the stage can be torn down by the scene builder.</summary>
        public static void Destroy(Object item)
        {
            if (item == null) return;
            if (Application.isPlaying) Object.Destroy(item);
            else Object.DestroyImmediate(item);
        }

        public static GameObject Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        public static MeshRenderer Renderer(GameObject go, Mesh mesh, Material material)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }
    }
}
