// SpellIR: the compiled spell. Every value the cast reads is resolved here,
// with units, so nothing below the compiler interprets a rune.

using System.Collections.Generic;
using MagicCircleSim.Design;

namespace MagicCircleSim.Compiler
{
    public enum DiagnosticKey { MissingCenter, MultipleShapeRunes, DuplicateRune, UnknownRune, AngleIgnored, Defaulted }

    public enum Severity { Error, Info }

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

    /// <summary>Vòng 3: where the spell appears.</summary>
    public readonly struct SpellShape
    {
        public readonly FormValue Form;
        /// <summary>Meters. Side length for a square (A04).</summary>
        public readonly double Radius;
        /// <summary>Share of a full turn, 0..1 (A01).</summary>
        public readonly double AngleFraction;
        public readonly int Count;

        public SpellShape(FormValue form, double radius, double angleFraction, int count)
        {
            Form = form;
            Radius = radius;
            AngleFraction = angleFraction;
            Count = count;
        }
    }

    /// <summary>Vòng 4: how what appeared moves.</summary>
    public readonly struct SpellMotion
    {
        public readonly PathValue Path;
        /// <summary>Seconds (A02).</summary>
        public readonly double LifetimeS;
        public readonly DistributionValue Distribution;
        /// <summary>Meters per second (A03).</summary>
        public readonly double Speed;

        public SpellMotion(PathValue path, double lifetimeS, DistributionValue distribution, double speed)
        {
            Path = path;
            LifetimeS = lifetimeS;
            Distribution = distribution;
            Speed = speed;
        }
    }

    /// <summary>A slot the circle can leave empty, in the order the summary lists them.</summary>
    public enum DefaultedSlot { Nature, Pattern, Form, Radius, Angle, Count, Path, Lifetime, Distribution, Speed }

    public sealed class SpellIR
    {
        public bool Valid;
        public IReadOnlyList<Diagnostic> Diagnostics;
        public ElementId? Element;
        public NatureId Nature;
        public MaterialId? Material;
        public PatternId Pattern;
        public SpellShape Shape;
        public SpellMotion Motion;
        /// <summary>Slots the circle left empty, filled from Defaults.</summary>
        public IReadOnlyList<DefaultedSlot> Defaulted;
        /// <summary>Seeds the layout and the particles, so one circle always casts the same way.</summary>
        public uint Seed;

        public bool WasDefaulted(DefaultedSlot slot)
        {
            foreach (var s in Defaulted)
                if (s == slot) return true;
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
