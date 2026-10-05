// The look table: how each material reads on screen and how its matter moves.
// Pure data, one row per material, so new art for an element is one row edit and
// never a branch in the particle or visual code. These are the simulator's
// interpretation, not design canon.
//
// The motion columns follow the web reference's per-element physics
// (wha-spell-simulator, volume/elements.ts): fire is buoyant, pinched onto its
// column and torn at the crown; water is thrown up and falls; wind swirls;
// light shoots straight and dies fast. The material moves the same way whether
// it stands on the circle, fills an area or rides an instance.

using System;
using MagicCircleSim.Design;

namespace MagicCircleSim.Cast
{
    /// <summary>
    /// How a material's particle sprite is drawn. The atlas itself is art, drawn by
    /// Art~/generate_particles.py and named after the material's key.
    /// </summary>
    public sealed class ParticleSprite
    {
        /// <summary>Frames per side of the atlas: 1, 2 or 4.</summary>
        public readonly int Grid;
        /// <summary>True plays the frames over the particle's life (fire). False picks one frame per particle.</summary>
        public readonly bool Animated;
        /// <summary>Random starting roll, radians. Zero keeps the sprite upright, π lets it face any way.</summary>
        public readonly double Tumble;
        /// <summary>Roll over the particle's life, radians, in a random direction per particle.</summary>
        public readonly double Spin;
        /// <summary>Additive light (fire, lightning) or alpha-blended matter (stone, leaves).</summary>
        public readonly bool Additive;

        public ParticleSprite(int grid, bool animated, double tumble, double spin, bool additive)
        {
            Grid = grid;
            Animated = animated;
            Tumble = tumble;
            Spin = spin;
            Additive = additive;
        }

        public static ParticleSprite Light(int grid, double tumble = 0, double spin = 0, bool animated = false) =>
            new ParticleSprite(grid, animated, tumble, spin, true);

        public static ParticleSprite Matter(int grid, double tumble = 0, double spin = 0) =>
            new ParticleSprite(grid, false, tumble, spin, false);
    }

    public sealed class Look
    {
        // Paint.

        /// <summary>Hex color of the instance's solid core (only drawn when Vòng 2 gives one, A18).</summary>
        public readonly string Core;
        /// <summary>Hex particle color at birth and at death.</summary>
        public readonly string Birth, Death;
        /// <summary>Core size multipliers along x, y and the heading. Laser is long and thin.</summary>
        public readonly double ScaleX, ScaleY, ScaleZ;
        public readonly double CoreOpacity;
        /// <summary>Particle diameter at birth, meters.</summary>
        public readonly double Size;
        /// <summary>Size at death as a multiple of <see cref="Size"/>. Flame tips shrink, steam billows.</summary>
        public readonly double Grow;
        /// <summary>Particle alpha, 0..1. Dense additive matter like fire needs less per particle.</summary>
        public readonly double Opacity;
        public readonly ParticleSprite Sprite;
        /// <summary>The phenomenon's own form, for materials that are a shape rather than a spray (Bodies.cs).</summary>
        public readonly BodySpec Body;

        // Emission.

        /// <summary>Particles per second per source: per instance, or per mouth-sized patch of a filled area.</summary>
        public readonly double Rate;
        /// <summary>Particle life, seconds.</summary>
        public readonly double Life;
        /// <summary>Radius of the disc a source emits from, meters. Also the column the pinch holds matter to.</summary>
        public readonly double Mouth;
        /// <summary>Launch speed straight up, m/s.</summary>
        public readonly double Rise;
        /// <summary>Random launch speed in every direction, m/s.</summary>
        public readonly double Spread;
        /// <summary>0..1 chance-weighted brightness drop per step. Lightning flickers hard.</summary>
        public readonly double Flicker;

        // Motion.

        /// <summary>Vertical acceleration, m/s². Positive is buoyancy (fire, steam), negative weight (water, stone).</summary>
        public readonly double Lift;
        /// <summary>Velocity lost per second, 0..1.</summary>
        public readonly double Drag;
        /// <summary>Coherent swirling acceleration, m/s². Flames lick, wind curls, stone does not.</summary>
        public readonly double Turbulence;
        /// <summary>Uncorrelated random acceleration, m/s². Lightning crackles.</summary>
        public readonly double Jitter;
        /// <summary>Pull back onto the source's column once outside its mouth, 1/s². Fire keeps a column, water does not.</summary>
        public readonly double Pinch;
        /// <summary>
        /// The column's radius at the tear height, as a multiple of the mouth. Below one a
        /// flame tapers to its tip; above one a whirlwind or a plume of steam flares open.
        /// </summary>
        public readonly double Flare;
        /// <summary>Spin about the source's vertical axis, rad/s. A whirlwind's identity.</summary>
        public readonly double Swirl;
        /// <summary>Meters above the source where the crown tears: matter ages fast there. Zero never tears.</summary>
        public readonly double Height;

        public Look(string core, string birth, string death, double scaleX, double scaleY, double scaleZ, double coreOpacity,
            ParticleSprite sprite, double size, double grow, double opacity,
            double rate, double life, double mouth, double rise, double spread, double flicker,
            double lift, double drag, double turbulence, double jitter, double pinch, double flare, double swirl, double height, BodySpec body = null)
        {
            Core = core;
            Birth = birth;
            Death = death;
            ScaleX = scaleX;
            ScaleY = scaleY;
            ScaleZ = scaleZ;
            CoreOpacity = coreOpacity;
            Size = size;
            Grow = grow;
            Opacity = opacity;
            Sprite = sprite;
            Body = body ?? BodySpec.None;
            Rate = rate;
            Life = life;
            Mouth = mouth;
            Rise = rise;
            Spread = spread;
            Flicker = flicker;
            Lift = lift;
            Drag = drag;
            Turbulence = turbulence;
            Jitter = jitter;
            Pinch = pinch;
            Flare = flare;
            Swirl = swirl;
            Height = height;
        }
    }

    public static class Looks
    {
        /// <summary>Indexed by MaterialId.</summary>
        static readonly Look[] Rows =
        {
            // Gió: wind streaming around its source, streaks curling at the head (body); a few motes ride it.
            new Look("#d8fff0", "#c8fff0", "#4fa98a", 1, 1, 1, 0.35,
                sprite: ParticleSprite.Light(2, tumble: Math.PI, spin: 3),
                size: 0.45, grow: 1.8, opacity: 0.2,
                rate: 40, life: 1.1, mouth: 0.6, rise: 2.2, spread: 0.8, flicker: 0,
                lift: 0.5, drag: 0.6, turbulence: 9, jitter: 6, pinch: 1.2, flare: 1.6, swirl: 2.5, height: 2.4,
                body: BodySpec.Gust(height: 2.2, innerRadius: 0.9, outerRadius: 2.6, speed: 8, streaks: 18, cycleS: 1.6, width: 0.17)),
            // Dây leo: thin strands creeping upward, twisting as they climb.
            new Look("#6fc25a", "#a6f07c", "#2f6b2a", 0.7, 0.7, 1.6, 0.9,
                sprite: ParticleSprite.Matter(2, tumble: Math.PI, spin: 2),
                size: 0.22, grow: 0.7, opacity: 0.7,
                rate: 90, life: 2.2, mouth: 0.35, rise: 1.4, spread: 0.15, flicker: 0,
                lift: -0.3, drag: 0.5, turbulence: 2.5, jitter: 0.3, pinch: 3, flare: 0.5, swirl: 1.6, height: 2.2),
            // Nước: a fountain thrown up that falls back and splashes.
            new Look("#5aa9ff", "#bfe3ff", "#1f5fb3", 1, 1, 1, 0.75,
                sprite: ParticleSprite.Matter(2, tumble: 0.3),
                size: 0.28, grow: 1.2, opacity: 0.55,
                rate: 220, life: 1.4, mouth: 0.25, rise: 6, spread: 1, flicker: 0,
                lift: -9, drag: 0.2, turbulence: 0.8, jitter: 0.5, pinch: 0, flare: 1, swirl: 0, height: 0),
            // Lửa: a buoyant flame column, pinched at the foot, licking and torn at the crown.
            new Look("#ffb347", "#ffd77a", "#e0401a", 1, 1, 1, 0.9,
                sprite: ParticleSprite.Light(4, tumble: 0.15, animated: true),
                size: 1.2, grow: 0.45, opacity: 0.3,
                rate: 420, life: 1, mouth: 0.65, rise: 1.2, spread: 0.5, flicker: 0.25,
                lift: 9, drag: 1.8, turbulence: 9, jitter: 1.5, pinch: 5, flare: 0.4, swirl: 0.6, height: 2.6),
            // Cát, Đất: a heave of sand that falls and piles.
            new Look("#c49556", "#e6c48a", "#6b4a22", 1, 1, 1, 0.95,
                sprite: ParticleSprite.Matter(2, tumble: Math.PI, spin: 2),
                size: 0.16, grow: 1, opacity: 0.8,
                rate: 200, life: 1.6, mouth: 0.5, rise: 3.5, spread: 1.4, flicker: 0,
                lift: -10, drag: 0.3, turbulence: 0.4, jitter: 0.3, pinch: 0, flare: 1, swirl: 0.8, height: 0),
            // Lốc: a whirlwind funnel coiling about a swaying axis (body); dust churns at its foot.
            new Look("#bfe8e6", "#e9fffd", "#5c8f8c", 0.9, 1.6, 0.9, 0.4,
                sprite: ParticleSprite.Light(2, tumble: Math.PI, spin: 8),
                size: 0.3, grow: 1.8, opacity: 0.18,
                rate: 140, life: 1.1, mouth: 0.5, rise: 1.2, spread: 0.6, flicker: 0,
                lift: 0.8, drag: 0.6, turbulence: 3, jitter: 4, pinch: 2, flare: 3, swirl: 7, height: 1.6,
                body: BodySpec.Funnel(height: 5, baseRadius: 0.12, topRadius: 2.6, spin: 4, strands: 36, width: 0.34)),
            // Thân cứng: a trunk growing straight up, slowly.
            new Look("#7a5a34", "#b9d58a", "#4a3a22", 0.6, 0.6, 1.9, 1,
                sprite: ParticleSprite.Matter(2, tumble: Math.PI, spin: 1.5),
                size: 0.3, grow: 0.8, opacity: 0.8,
                rate: 70, life: 2.6, mouth: 0.25, rise: 1.1, spread: 0.08, flicker: 0,
                lift: 0, drag: 0.4, turbulence: 0.6, jitter: 0.2, pinch: 8, flare: 0.8, swirl: 0.2, height: 2.6),
            // Băng: shards thrown out that stop hard and settle.
            new Look("#e4f6ff", "#ffffff", "#7fc4ff", 0.9, 0.9, 1.2, 0.85,
                sprite: ParticleSprite.Matter(2, tumble: Math.PI, spin: 1),
                size: 0.18, grow: 0.6, opacity: 0.75,
                rate: 120, life: 2, mouth: 0.5, rise: 2.5, spread: 1, flicker: 0.1,
                lift: -1.5, drag: 2.4, turbulence: 0.2, jitter: 0.3, pinch: 0, flare: 1, swirl: 0, height: 0),
            // Laser: a beam straight up, pinched tight, gone the moment it stops.
            new Look("#ff5a7a", "#ffd0dc", "#ff2050", 0.22, 0.22, 4, 1,
                sprite: ParticleSprite.Light(1),
                size: 0.22, grow: 0.6, opacity: 0.6,
                rate: 260, life: 0.45, mouth: 0.1, rise: 14, spread: 0.05, flicker: 0.1,
                lift: 0, drag: 0, turbulence: 0, jitter: 0, pinch: 30, flare: 1, swirl: 0, height: 6),
            // Đá: heavy clods lobbed up that drop hard.
            new Look("#8c8a86", "#c9c4bb", "#4d4b47", 1.25, 1.25, 1.25, 1,
                sprite: ParticleSprite.Matter(2, tumble: Math.PI, spin: 2.5),
                size: 0.32, grow: 1, opacity: 0.9,
                rate: 60, life: 1.8, mouth: 0.6, rise: 4.5, spread: 1.6, flicker: 0,
                lift: -12, drag: 0.2, turbulence: 0, jitter: 0, pinch: 0, flare: 1, swirl: 0, height: 0),
            // Sóng âm: wavefronts spreading from the source as expanding shells (body).
            new Look("#c7a6ff", "#efe3ff", "#6a3fc4", 1, 1, 1, 0.3,
                sprite: ParticleSprite.Light(1),
                size: 0.12, grow: 1, opacity: 0.6,
                rate: 20, life: 0.8, mouth: 0.2, rise: 0.3, spread: 5, flicker: 0,
                lift: 0, drag: 1.5, turbulence: 0, jitter: 0, pinch: 0, flare: 1, swirl: 0, height: 0,
                body: BodySpec.Waves(reach: 7, speed: 6, periodS: 0.3, rings: 3, width: 0.07)),
            // Dạng hoa: petals tossed up that drift and flutter down.
            new Look("#ff9ccf", "#ffe0f0", "#e0508f", 1, 1, 1, 0.85,
                sprite: ParticleSprite.Matter(2, tumble: Math.PI, spin: 3),
                size: 0.22, grow: 1, opacity: 0.7,
                rate: 90, life: 2.4, mouth: 0.4, rise: 2.6, spread: 1.2, flicker: 0,
                lift: -0.9, drag: 0.9, turbulence: 3.5, jitter: 1, pinch: 0, flare: 1, swirl: 1.4, height: 0),
            // Hơi nước: steam rising slowly and billowing out.
            new Look("#f2f6fa", "#ffffff", "#9aa8b8", 1.1, 1.1, 1.1, 0.3,
                sprite: ParticleSprite.Matter(2, tumble: Math.PI, spin: 0.8),
                size: 0.7, grow: 2.6, opacity: 0.22,
                rate: 140, life: 2, mouth: 0.45, rise: 1.2, spread: 0.4, flicker: 0,
                lift: 1.6, drag: 0.8, turbulence: 3, jitter: 0.6, pinch: 1, flare: 2.2, swirl: 0.4, height: 2.8),
            // Lôi: a crackling column, short-lived and violently jittered.
            new Look("#fff6a0", "#ffffff", "#8a6bff", 0.9, 0.9, 1.4, 1,
                sprite: ParticleSprite.Light(4, tumble: 0.3),
                size: 1.6, grow: 1.1, opacity: 0.9,
                rate: 45, life: 0.16, mouth: 0.5, rise: 5, spread: 1.5, flicker: 0.8,
                lift: 0, drag: 0, turbulence: 0, jitter: 20, pinch: 2, flare: 1.5, swirl: 0, height: 3),
            // Kim loại: sparks thrown up that fall and bounce.
            new Look("#d6dbe3", "#ffffff", "#ffb24a", 1, 1, 1, 1,
                sprite: ParticleSprite.Light(1, tumble: Math.PI / 4),
                size: 0.1, grow: 0.4, opacity: 0.9,
                rate: 140, life: 0.9, mouth: 0.3, rise: 5, spread: 3, flicker: 0.15,
                lift: -9, drag: 0.4, turbulence: 0, jitter: 0, pinch: 0, flare: 1, swirl: 0, height: 0),
        };

        /// <summary>The material's row, or null when the table is missing one (CastTests).</summary>
        public static Look Of(MaterialId id) => (int)id < Rows.Length ? Rows[(int)id] : null;
    }
}
