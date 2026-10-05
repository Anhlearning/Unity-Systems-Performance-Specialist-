// FNV-1a, bit-for-bit the web build's hash.

namespace MagicCircleSim.Compiler
{
    /// <summary>FNV-1a over a string's UTF-16 code units. Seeds a cast from what is on the circle.</summary>
    public static class Fnv1a
    {
        public static uint Hash(string text)
        {
            unchecked
            {
                var hash = 0x811c9dc5u;
                foreach (var c in text)
                {
                    hash ^= c;
                    hash *= 0x01000193;
                }
                return hash;
            }
        }
    }
}
