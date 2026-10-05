// The magic circle painted on the ground under the caster, from the same
// drawing the editor shows. Its glow follows the cast clock.

using System;
using UnityEngine;

namespace MagicCircleSim.View
{
    public sealed class GroundCircle : IDisposable
    {
        /// <summary>Diameter on the ground, meters.</summary>
        const float SizeM = 5;

        readonly MeshRenderer renderer;
        readonly Mesh mesh;
        readonly MaterialPropertyBlock props = new MaterialPropertyBlock();

        public GroundCircle(Transform stage, StageAssets assets, Texture texture)
        {
            mesh = StageMeshes.GroundQuad(SizeM);
            var go = ViewUtil.Child(stage, "Ground Circle");
            go.transform.localPosition = new Vector3(0, 0.01f, 0);
            renderer = ViewUtil.Renderer(go, mesh, assets.Additive);
            props.SetTexture(StageAssets.MainTexId, texture);
            SetGlow(0.3f);
        }

        public void SetGlow(float level)
        {
            props.SetColor(StageAssets.ColorId, new Color(1, 1, 1, 0.15f + 0.85f * level));
            renderer.SetPropertyBlock(props);
        }

        public void Dispose() => ViewUtil.Destroy(mesh);
    }
}
