// Everything one cast draws: a glow per instance and a core mesh when Vòng 2
// gives it one (A18), the particles, and the Vòng 3 area outline when there is
// an area. It reads a SpellPerformance each frame and owns no simulation state
// of its own.
//
// Caster frame to stage: (x, y, z) maps to local (x, y, z). Unity is left
// handed with +Z forward, so unlike the three.js build no axis flips.

using System;
using MagicCircleSim.Cast;
using MagicCircleSim.Compiler;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicCircleSim.View
{
    public sealed class SpellVisuals : IDisposable
    {
        const float GlowM = 1.6f;
        const float OutlineY = 0.03f;
        const float OutlineWidthM = 0.05f;
        /// <summary>A glow on the ground sits this high and this much wider, so it lights the foot of the matter.</summary>
        const float RootedGlowY = 0.5f;
        const float RootedGlowScale = 1.4f;
        const float RootedGlowAlpha = 0.3f;

        readonly SpellPerformance perf;
        readonly GameObject root;
        readonly Mesh coreMesh;
        readonly Transform[] cores;
        readonly MeshRenderer[] coreRenderers;
        readonly Transform[] glows;
        readonly MeshRenderer[] glowRenderers;
        readonly LineRenderer outline;
        readonly ParticleRenderer particles;
        /// <summary>The phenomenon's own form, when its material has one (Bodies.cs).</summary>
        readonly StrokeRenderer body;
        readonly MaterialPropertyBlock props = new MaterialPropertyBlock();
        readonly Color coreColor;
        readonly Color glowColor;
        readonly Vector3 coreScale;

        public SpellVisuals(SpellPerformance perf, Transform stage, StageAssets assets, Mesh glowQuad)
        {
            this.perf = perf;
            var look = perf.Look;
            coreColor = StageAssets.ParseColor(look.Core);
            glowColor = StageAssets.ParseColor(look.Birth);
            coreScale = new Vector3((float)look.ScaleX, (float)look.ScaleY, (float)look.ScaleZ);
            root = ViewUtil.Child(stage, "Spell");

            var pattern = perf.Spell.Pattern;
            coreMesh = pattern.HasValue ? StageMeshes.Core(pattern.Value) : null;
            var count = perf.Spawns.Length;
            cores = new Transform[count];
            coreRenderers = new MeshRenderer[count];
            glows = new Transform[count];
            glowRenderers = new MeshRenderer[count];
            for (var i = 0; i < count; i++)
            {
                if (coreMesh != null)
                {
                    var core = ViewUtil.Child(root.transform, $"Core {i}");
                    coreRenderers[i] = ViewUtil.Renderer(core, coreMesh, assets.AlphaBlend);
                    cores[i] = core.transform;
                }
                var glow = ViewUtil.Child(root.transform, $"Glow {i}");
                glowRenderers[i] = ViewUtil.Renderer(glow, glowQuad, assets.Glow);
                glows[i] = glow.transform;
            }

            if (perf.Spell.Shape.HasValue) outline = BuildOutline(root.transform, assets, perf.Spell.Shape.Value);
            var material = perf.Spell.Material.Value;
            var bodySpec = look.Body;
            if (bodySpec.Kind != BodyKind.None)
            {
                body = new StrokeRenderer(root.transform, stage, bodySpec.Additive ? assets.Additive : assets.AlphaBlend,
                    assets.BodyTexture(bodySpec.Texture), StageAssets.ParseColor(look.Birth));
            }
            particles = new ParticleRenderer(perf.Pool, look, look.Sprite.Additive ? assets.Particles : assets.ParticlesAlpha, assets.ParticleSprite(material));
        }

        static LineRenderer BuildOutline(Transform parent, StageAssets assets, SpellShape shape)
        {
            var points = RegionOutline.Of(shape);
            var line = ViewUtil.Child(parent, "Area Outline").AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = assets.AlphaBlend;
            line.widthMultiplier = OutlineWidthM;
            line.alignment = LineAlignment.TransformZ;
            line.transform.localRotation = Quaternion.Euler(90, 0, 0);
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = points.Count;
            // Rotated to lie flat, so caster (x, z) is the line's local (x, y).
            for (var i = 0; i < points.Count; i++) line.SetPosition(i, new Vector3((float)points[i].X, (float)points[i].Z, -OutlineY));
            return line;
        }

        /// <summary>Shows the area outline while the circle charges and the instances manifest.</summary>
        public void SetOutlineOpacity(float opacity)
        {
            if (outline == null) return;
            var color = glowColor;
            color.a = opacity;
            props.Clear();
            props.SetColor(StageAssets.ColorId, color);
            outline.SetPropertyBlock(props);
        }

        public void Sync(Camera viewer)
        {
            var look = perf.Look;
            var faceViewer = viewer != null ? viewer.transform.rotation : Quaternion.identity;
            var glowLift = perf.Rooted ? RootedGlowY : 0;
            var glowSize = GlowM * (perf.Rooted ? RootedGlowScale : 1);
            var glowAlpha = perf.Rooted ? RootedGlowAlpha : 0.7f;
            for (var i = 0; i < glows.Length; i++)
            {
                var state = perf.Instances[i];
                var visible = state.Alpha > 0.001;
                if (coreRenderers[i] != null) coreRenderers[i].enabled = visible;
                glowRenderers[i].enabled = visible;
                if (!visible) continue;

                var position = new Vector3((float)state.X, (float)state.Y, (float)state.Z);
                var scale = (float)state.Scale;
                var alpha = (float)state.Alpha;
                if (cores[i] != null)
                {
                    var core = cores[i];
                    core.localPosition = position;
                    core.localRotation = Quaternion.LookRotation(new Vector3((float)state.DirX, 0, (float)state.DirZ));
                    core.localScale = coreScale * scale;
                    SetColor(coreRenderers[i], coreColor, (float)look.CoreOpacity * alpha);
                }

                var glow = glows[i];
                glow.localPosition = position + Vector3.up * glowLift;
                glow.rotation = faceViewer;
                glow.localScale = Vector3.one * (glowSize * Mathf.Max(scale, 0.01f));
                SetColor(glowRenderers[i], glowColor, glowAlpha * alpha);
            }
            if (body != null)
            {
                perf.BuildStrokes();
                body.Render(perf.Strokes, viewer);
            }
            particles.Render(root.transform.localToWorldMatrix);
        }

        void SetColor(Renderer renderer, Color color, float alpha)
        {
            color.a = alpha;
            props.Clear();
            props.SetColor(StageAssets.ColorId, color);
            renderer.SetPropertyBlock(props);
        }

        public void Dispose()
        {
            particles.Dispose();
            body?.Dispose();
            ViewUtil.Destroy(root);
            ViewUtil.Destroy(coreMesh);
        }
    }
}
