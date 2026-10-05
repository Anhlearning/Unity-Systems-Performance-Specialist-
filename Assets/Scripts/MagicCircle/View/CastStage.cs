// CastStage: the scene a spell is performed in. It owns the ground circle, the
// floor grid and the current cast, and maps game time to cast time. All motion
// comes from SpellPerformance. This is the only place below the preview that
// reads a clock.
//
//   stage.SetCircle(circle);
//   stage.Cast(SpellCompiler.Compile(circle));

using System;
using MagicCircleSim.Cast;
using MagicCircleSim.Circle;
using MagicCircleSim.Compiler;
using UnityEngine;

namespace MagicCircleSim.View
{
    [DisallowMultipleComponent]
    public sealed class CastStage : MonoBehaviour
    {
        /// <summary>The circle stays faintly visible between casts.</summary>
        const float IdleGlow = 0.3f;
        const int CirclePx = 1024;
        const string GroundInk = "#ffffff";

        [SerializeField] Shader unlitShader;
        [SerializeField] Shader particleShader;
        [Tooltip("Glows face this camera. Falls back to Camera.main.")]
        [SerializeField] Camera viewer;

        StageAssets assets;
        GroundCircle ground;
        GameObject floor;
        Mesh floorMesh;
        Mesh glowQuad;
        SpellPerformance perf;
        SpellVisuals visuals;
        double startS;

        /// <summary>Raised when a cast has played out, so the preview can loop it.</summary>
        public event Action Finished;

        /// <summary>The circle as drawn on the ground. The editor panel shows the same texture.</summary>
        public CircleTexture Circle { get; private set; }

        public bool Casting => perf != null;

        public SpellPerformance Performance => perf;

        public Camera Viewer => viewer != null ? viewer : Camera.main;

        public void Configure(Shader unlit, Shader particles, Camera camera)
        {
            unlitShader = unlit;
            particleShader = particles;
            viewer = camera;
        }

        void Awake()
        {
            if (unlitShader == null) unlitShader = Shader.Find("MagicCircle/Unlit");
            if (particleShader == null) particleShader = Shader.Find("MagicCircle/Particles");
            assets = new StageAssets(unlitShader, particleShader);
            Circle = new CircleTexture(CirclePx);
            ground = new GroundCircle(transform, assets, Circle.Texture);
            glowQuad = StageMeshes.BillboardQuad();
            floorMesh = StageMeshes.Grid(80, 40, StageAssets.ParseColor("#2a3146"), StageAssets.ParseColor("#1a1f2e"));
            floor = ViewUtil.Child(transform, "Floor Grid");
            ViewUtil.Renderer(floor, floorMesh, assets.Floor);
            SetCircle(MagicCircle.Empty);
        }

        public void SetCircle(MagicCircle circle) => Circle.Draw(DrawCircle.Build(circle, GroundInk));

        /// <summary>Performs the spell as it is now. An invalid spell just clears the stage.</summary>
        public void Cast(SpellIR spell)
        {
            ClearSpell();
            if (!spell.Valid) return;
            perf = new SpellPerformance(spell);
            visuals = new SpellVisuals(perf, transform, assets, glowQuad);
            visuals.SetOutlineOpacity(0);
            startS = Time.timeAsDouble;
        }

        void Update()
        {
            if (perf == null) return;
            var tS = Time.timeAsDouble - startS;
            perf.AdvanceTo(tS);
            visuals.Sync(Viewer);
            visuals.SetOutlineOpacity((float)CastTimeline.OutlineOpacityAt(tS));
            ground.SetGlow((float)CastTimeline.CircleGlowAt(tS, perf.Spell.Motion.LifetimeS));
            if (!perf.Done) return;
            ClearSpell();
            Finished?.Invoke();
        }

        void ClearSpell()
        {
            visuals?.Dispose();
            visuals = null;
            perf = null;
            ground?.SetGlow(IdleGlow);
        }

        void OnDestroy()
        {
            ClearSpell();
            ground?.Dispose();
            Circle?.Dispose();
            ViewUtil.Destroy(floor);
            ViewUtil.Destroy(floorMesh);
            ViewUtil.Destroy(glowQuad);
            assets?.Dispose();
        }
    }
}
