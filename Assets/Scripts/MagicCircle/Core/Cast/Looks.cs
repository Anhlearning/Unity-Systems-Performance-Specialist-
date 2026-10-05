// The look table: how each material reads on screen. Pure data, one row per
// material, so new art for an element is one row edit and never a branch in the
// particle or visual code. These are the simulator's interpretation, not design
// canon.

using MagicCircleSim.Design;

namespace MagicCircleSim.Cast
{
    public sealed class Look
    {
        /// <summary>Hex color of the instance's solid core.</summary>
        public readonly string Core;
        /// <summary>Hex particle color at birth and at death.</summary>
        public readonly string Birth, Death;
        /// <summary>Core size multipliers along x, y and the heading. Laser is long and thin.</summary>
        public readonly double ScaleX, ScaleY, ScaleZ;
        public readonly double CoreOpacity;
        /// <summary>Particles per second per instance.</summary>
        public readonly double Rate;
        /// <summary>Particle life, seconds.</summary>
        public readonly double Life;
        /// <summary>Particle diameter, meters.</summary>
        public readonly double Size;
        /// <summary>Random launch speed, m/s.</summary>
        public readonly double Spread;
        /// <summary>Vertical acceleration, m/s². Positive rises (fire, steam), negative falls (water, stone).</summary>
        public readonly double Lift;
        /// <summary>Velocity lost per second, 0..1.</summary>
        public readonly double Drag;
        /// <summary>Random acceleration, m/s². Wind and lightning churn, stone does not.</summary>
        public readonly double Jitter;
        /// <summary>0..1 chance-weighted brightness drop per step. Lightning flickers hard.</summary>
        public readonly double Flicker;

        public Look(string core, string birth, string death, double scaleX, double scaleY, double scaleZ, double coreOpacity,
            double rate, double life, double size, double spread, double lift, double drag, double jitter, double flicker)
        {
            Core = core;
            Birth = birth;
            Death = death;
            ScaleX = scaleX;
            ScaleY = scaleY;
            ScaleZ = scaleZ;
            CoreOpacity = coreOpacity;
            Rate = rate;
            Life = life;
            Size = size;
            Spread = spread;
            Lift = lift;
            Drag = drag;
            Jitter = jitter;
            Flicker = flicker;
        }
    }

    public static class Looks
    {
        /// <summary>Indexed by MaterialId. Columns: core, birth, death, scale x/y/z, opacity, rate, life, size, spread, lift, drag, jitter, flicker.</summary>
        static readonly Look[] Rows =
        {
            /* Gio      */ new Look("#d8fff0", "#c8fff0", "#4fa98a", 1, 1, 1, 0.35, 90, 0.9, 0.35, 2.5, 0.5, 0.6, 14, 0),
            /* DayLeo   */ new Look("#6fc25a", "#a6f07c", "#2f6b2a", 0.7, 0.7, 1.6, 0.9, 45, 1.6, 0.25, 0.6, -0.8, 0.4, 2, 0),
            /* Nuoc     */ new Look("#5aa9ff", "#bfe3ff", "#1f5fb3", 1, 1, 1, 0.75, 80, 1.1, 0.3, 1.6, -7, 0.2, 1, 0),
            /* Lua      */ new Look("#ffb347", "#fff1a8", "#d8321c", 1, 1, 1, 0.9, 110, 0.75, 0.45, 1.2, 4.5, 0.8, 6, 0.25),
            /* CatDat   */ new Look("#c49556", "#e6c48a", "#6b4a22", 1, 1, 1, 0.95, 120, 0.9, 0.16, 2.2, -9, 0.3, 0.5, 0),
            /* Loc      */ new Look("#bfe8e6", "#e9fffd", "#5c8f8c", 0.9, 1.6, 0.9, 0.4, 130, 0.8, 0.3, 3.5, 2.5, 0.5, 22, 0),
            /* ThanCung */ new Look("#7a5a34", "#b9d58a", "#4a3a22", 0.6, 0.6, 1.9, 1, 25, 1.4, 0.22, 0.5, -3, 0.3, 0.5, 0),
            /* Bang     */ new Look("#e4f6ff", "#ffffff", "#7fc4ff", 0.9, 0.9, 1.2, 0.85, 60, 1.5, 0.14, 0.9, -1.5, 1.2, 0.5, 0.1),
            /* Laser    */ new Look("#ff5a7a", "#ffd0dc", "#ff2050", 0.22, 0.22, 4, 1, 70, 0.35, 0.18, 0.4, 0, 2, 0, 0.1),
            /* Da       */ new Look("#8c8a86", "#c9c4bb", "#4d4b47", 1.25, 1.25, 1.25, 1, 35, 0.9, 0.2, 1.4, -10, 0.2, 0, 0),
            /* SongAm   */ new Look("#c7a6ff", "#efe3ff", "#6a3fc4", 1, 1, 1, 0.3, 50, 0.5, 0.9, 5, 0, 1.5, 0, 0),
            /* DangHoa  */ new Look("#ff9ccf", "#ffe0f0", "#e0508f", 1, 1, 1, 0.85, 55, 2, 0.22, 1.4, -0.9, 0.6, 3.5, 0),
            /* HoiNuoc  */ new Look("#f2f6fa", "#ffffff", "#9aa8b8", 1.1, 1.1, 1.1, 0.3, 70, 1.8, 0.85, 0.8, 2, 0.6, 2, 0),
            /* Loi      */ new Look("#fff6a0", "#ffffff", "#8a6bff", 0.9, 0.9, 1.4, 1, 140, 0.18, 0.2, 9, 0, 0, 40, 0.8),
            /* KimLoai  */ new Look("#d6dbe3", "#ffffff", "#ffb24a", 1, 1, 1, 1, 45, 0.6, 0.1, 3.5, -6, 0.4, 0, 0.15),
        };

        /// <summary>The material's row, or null when the table is missing one (CastTests).</summary>
        public static Look Of(MaterialId id) => (int)id < Rows.Length ? Rows[(int)id] : null;
    }
}
