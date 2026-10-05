// The particle pool drawn as one procedural draw. The pool's float arrays are
// uploaded as-is every frame and the shader billboards each particle with a
// frame of its material's sprite atlas, so the CPU never expands particles into
// vertices and nothing allocates per frame.

using System;
using MagicCircleSim.Cast;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicCircleSim.View
{
    public sealed class ParticleRenderer : IDisposable
    {
        static readonly int PositionsId = Shader.PropertyToID("_Positions");
        static readonly int RatiosId = Shader.PropertyToID("_Ratios");
        static readonly int SizesId = Shader.PropertyToID("_Sizes");
        static readonly int BirthId = Shader.PropertyToID("_Birth");
        static readonly int DeathId = Shader.PropertyToID("_Death");
        static readonly int GrowId = Shader.PropertyToID("_Grow");
        static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        static readonly int GridId = Shader.PropertyToID("_Grid");
        static readonly int AnimatedId = Shader.PropertyToID("_Animated");
        static readonly int TumbleId = Shader.PropertyToID("_Tumble");
        static readonly int SpinId = Shader.PropertyToID("_Spin");
        static readonly int StageToWorldId = Shader.PropertyToID("_StageToWorld");

        /// <summary>Generous bounds so the draw is never culled while particles drift.</summary>
        const float BoundsM = 200;

        readonly ParticlePool pool;
        readonly GraphicsBuffer positions;
        readonly GraphicsBuffer ratios;
        readonly GraphicsBuffer sizes;
        readonly MaterialPropertyBlock props = new MaterialPropertyBlock();
        RenderParams renderParams;

        public ParticleRenderer(ParticlePool pool, Look look, Material material, Texture sprite)
        {
            this.pool = pool;
            positions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pool.Capacity * 3, sizeof(float));
            ratios = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pool.Capacity, sizeof(float));
            sizes = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pool.Capacity, sizeof(float));
            props.SetBuffer(PositionsId, positions);
            props.SetBuffer(RatiosId, ratios);
            props.SetBuffer(SizesId, sizes);
            props.SetColor(BirthId, StageAssets.ParseColor(look.Birth));
            props.SetColor(DeathId, StageAssets.ParseColor(look.Death));
            props.SetFloat(GrowId, (float)look.Grow);
            props.SetFloat(OpacityId, (float)look.Opacity);
            props.SetTexture(StageAssets.MainTexId, sprite);
            props.SetFloat(GridId, look.Sprite.Grid);
            props.SetFloat(AnimatedId, look.Sprite.Animated ? 1 : 0);
            props.SetFloat(TumbleId, (float)look.Sprite.Tumble);
            props.SetFloat(SpinId, (float)look.Sprite.Spin);
            renderParams = new RenderParams(material)
            {
                matProps = props,
                worldBounds = new Bounds(Vector3.zero, Vector3.one * BoundsM),
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
            };
        }

        /// <summary>Uploads the pool and queues this frame's draw. Call once per frame.</summary>
        public void Render(Matrix4x4 stageToWorld)
        {
            positions.SetData(pool.Positions);
            ratios.SetData(pool.Ratios);
            sizes.SetData(pool.Sizes);
            props.SetMatrix(StageToWorldId, stageToWorld);
            renderParams.worldBounds = new Bounds(stageToWorld.GetPosition(), Vector3.one * BoundsM);
            Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, pool.Capacity * 6);
        }

        public void Dispose()
        {
            positions.Dispose();
            ratios.Dispose();
            sizes.Dispose();
        }
    }
}
