// The only randomness in the simulator: a seeded mulberry32, bit-for-bit the
// web build's. Never use System.Random or UnityEngine.Random below the stage.

namespace MagicCircleSim.Compiler
{
    public sealed class Mulberry32
    {
        uint state;

        public Mulberry32(uint seed) => state = seed;

        /// <summary>A double in [0, 1).</summary>
        public double Next()
        {
            unchecked
            {
                state += 0x6d2b79f5;
                var t = state;
                t = (t ^ (t >> 15)) * (t | 1);
                t ^= t + (t ^ (t >> 7)) * (t | 61);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }
}
