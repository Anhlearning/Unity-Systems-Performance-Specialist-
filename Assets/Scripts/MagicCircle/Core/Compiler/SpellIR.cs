// SpellIR: the compiled spell. Every value the cast reads is resolved here,
// with units, so nothing below the compiler interprets a rune. A slot the circle
// leaves empty stays empty (A09): the spell simply lacks that property.

using System.Collections.Generic;
using MagicCircleSim.Design;

namespace MagicCircleSim.Compiler
{
    public enum DiagnosticKey
    {
        MissingCenter, MissingNature, MultipleShapeRunes, DuplicateRune, UnknownRune,
        AngleIgnored, NeedsForm, NeedsCount, PatternNeedsCount, MotionIncomplete,
    }

    /// <summary>Error blocks the cast. Warning and Info explain a rune that has no effect yet.</summary>
    public enum Severity { Error, Warning, Info }

    public readonly struct Diagnostic
    {
        public readonly DiagnosticKey Key;
        public readonly Severity Severity;
        public readonly string Message;

        public Diagnostic(DiagnosticKey key, Severity severity, string message)
        {
            Key = key;
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>Vòng 3: where the spell appears. Only exists when a form rune is placed (A15).</summary>
    public readonly struct SpellShape
    {
        public readonly FormValue Form;
        /// <summary>Meters. Side length for a square (A04). The circle's own radius when no rune sets it (A16).</summary>
        public readonly double Radius;
        /// <summary>Share of a full turn, 0..1 (A01). One, the whole shape, when no rune narrows it.</summary>
        public readonly double AngleFraction;
        /// <summary>How many instances. Null when the material fills the whole area instead (A17).</summary>
        public readonly int? Count;

        public SpellShape(FormValue form, double radius, double angleFraction, int? count)
        {
            Form = form;
            Radius = radius;
            AngleFraction = angleFraction;
            Count = count;
        }

        public bool Fills => !Count.HasValue;
    }

    /// <summary>Vòng 4: how what appeared moves.</summary>
    public readonly struct SpellMotion
    {
        /// <summary>Null when the spell stands still (A19).</summary>
        public readonly PathValue? Path;
        /// <summary>Seconds (A02). Null when the spell lasts until it is stopped (A20).</summary>
        public readonly double? LifetimeS;
        /// <summary>The rune, or Even when several instances need a layout (V4_RULE_001).</summary>
        public readonly DistributionValue Distribution;
        /// <summary>Meters per second (A03). Null exactly when <see cref="Path"/> is.</summary>
        public readonly double? Speed;

        public SpellMotion(PathValue? path, double? lifetimeS, DistributionValue distribution, double? speed)
        {
            Path = path;
            LifetimeS = lifetimeS;
            Distribution = distribution;
            Speed = speed;
        }

        public bool Moves => Path.HasValue && Speed.HasValue;

        public bool Sustained => !LifetimeS.HasValue;
    }

    public sealed class SpellIR
    {
        public bool Valid;
        public IReadOnlyList<Diagnostic> Diagnostics;
        public ElementId? Element;
        public NatureId? Nature;
        /// <summary>Vòng 1 × the center. Null until both are on the circle (A14).</summary>
        public MaterialId? Material;
        /// <summary>Null when Vòng 2 is empty: the material has no solid core (A18).</summary>
        public PatternId? Pattern;
        /// <summary>Null when Vòng 3 has no form rune: one manifestation at the circle's center (A15).</summary>
        public SpellShape? Shape;
        public SpellMotion Motion;
        /// <summary>The rune placed in each category, for the summary. Empty categories are absent.</summary>
        public IReadOnlyDictionary<RuneCategory, RuneDef> Placed;
        /// <summary>Categories whose rune is on the circle but has no effect yet.</summary>
        public IReadOnlyList<RuneCategory> Ignored;
        /// <summary>Seeds the layout and the particles, so one circle always casts the same way.</summary>
        public uint Seed;

        public RuneDef RuneIn(RuneCategory category) => Placed.TryGetValue(category, out var rune) ? rune : null;

        public bool IsIgnored(RuneCategory category)
        {
            foreach (var c in Ignored)
                if (c == category) return true;
            return false;
        }

        public bool Has(DiagnosticKey key)
        {
            foreach (var d in Diagnostics)
                if (d.Key == key) return true;
            return false;
        }
    }
}
