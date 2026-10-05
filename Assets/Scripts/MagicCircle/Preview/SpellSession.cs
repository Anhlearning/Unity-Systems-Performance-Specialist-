// SpellSession: the preview's one piece of state. The circle is the only thing
// stored. The compiled spell is derived from it.

using System;
using MagicCircleSim.Circle;
using MagicCircleSim.Compiler;
using MagicCircleSim.Design;

namespace MagicCircleSim.Preview
{
    public sealed class SpellSession
    {
        MagicCircle circle = MagicCircle.Empty;
        SpellIR spell;

        /// <summary>Raised after every edit, once the circle has its new value.</summary>
        public event Action Changed;

        public MagicCircle Circle
        {
            get => circle;
            private set
            {
                circle = value;
                spell = null;
                Changed?.Invoke();
            }
        }

        public SpellIR Spell => spell ??= SpellCompiler.Compile(circle);

        public void ToggleCenter(ElementId id) => Circle = circle.WithCenter(circle.Center == id ? (ElementId?)null : id);

        public void ToggleNature(NatureId id) => Circle = circle.WithNature(circle.Nature == id ? (NatureId?)null : id);

        public void TogglePattern(PatternId id) => Circle = circle.WithPattern(circle.Pattern == id ? (PatternId?)null : id);

        public void ToggleRune(string id) => Circle = circle.PlaceRune(id);

        public void RemoveRune(string id) => Circle = circle.RemoveRune(id);

        public void Load(MagicCircle preset) => Circle = preset;

        public void Clear() => Circle = MagicCircle.Empty;
    }
}
