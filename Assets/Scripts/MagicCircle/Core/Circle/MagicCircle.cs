// MagicCircle: what the player has put on the circle, and nothing about what it
// means. The compiler reads meaning from it. Hand-drawn recognition can later
// produce the same value without the rest of the app noticing.
//
//   var circle = MagicCircle.Empty.WithCenter(ElementId.Hoa).PlaceRune("form-ring");

using System;
using System.Collections.Generic;
using MagicCircleSim.Design;

namespace MagicCircleSim.Circle
{
    /// <summary>Immutable. Every edit returns a new circle.</summary>
    public sealed class MagicCircle
    {
        public static readonly MagicCircle Empty = new MagicCircle(null, null, null, Array.Empty<string>());

        public readonly ElementId? Center;
        /// <summary>Vòng 1's form.</summary>
        public readonly NatureId? Nature;
        /// <summary>Vòng 2's pattern.</summary>
        public readonly PatternId? Pattern;
        /// <summary>Rune ids in placement order. Their band follows from their category.</summary>
        public readonly IReadOnlyList<string> Runes;

        public MagicCircle(ElementId? center, NatureId? nature, PatternId? pattern, IEnumerable<string> runes)
        {
            Center = center;
            Nature = nature;
            Pattern = pattern;
            Runes = new List<string>(runes);
        }

        public MagicCircle WithCenter(ElementId? center) => new MagicCircle(center, Nature, Pattern, Runes);

        public MagicCircle WithNature(NatureId? nature) => new MagicCircle(Center, nature, Pattern, Runes);

        public MagicCircle WithPattern(PatternId? pattern) => new MagicCircle(Center, Nature, pattern, Runes);

        public MagicCircle WithRunes(IEnumerable<string> runes) => new MagicCircle(Center, Nature, Pattern, runes);

        /// <summary>
        /// Places a rune, replacing any rune of the same category (A10). Placing a
        /// rune that is already there takes it off, so a palette button works as a toggle.
        /// </summary>
        public MagicCircle PlaceRune(string runeId)
        {
            var rune = Design.Runes.Find(runeId);
            if (rune == null) return this;
            if (Contains(runeId)) return RemoveRune(runeId);
            var kept = new List<string>();
            foreach (var id in Runes)
                if (Design.Runes.Find(id)?.Category != rune.Category) kept.Add(id);
            kept.Add(runeId);
            return WithRunes(kept);
        }

        public MagicCircle RemoveRune(string runeId)
        {
            var kept = new List<string>();
            foreach (var id in Runes)
                if (id != runeId) kept.Add(id);
            return WithRunes(kept);
        }

        public bool Contains(string runeId)
        {
            foreach (var id in Runes)
                if (id == runeId) return true;
            return false;
        }

        public List<RuneDef> RunesOnBand(RuneBand band)
        {
            var onBand = new List<RuneDef>();
            foreach (var id in Runes)
            {
                var rune = Design.Runes.Find(id);
                if (rune != null && rune.Band == band) onBand.Add(rune);
            }
            return onBand;
        }
    }
}
