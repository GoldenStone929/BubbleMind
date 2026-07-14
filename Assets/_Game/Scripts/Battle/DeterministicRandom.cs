using System;

namespace GenericGachaRPG
{
    /// <summary>
    /// Small platform-independent xorshift generator for future data-driven
    /// targeting and combat rolls. Its sequence is stable for a given seed.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private const uint ZeroSeedFallback = 0x6D2B79F5u;
        private uint _state;

        public DeterministicRandom(int seed)
        {
            _state = unchecked((uint)seed);
            if (_state == 0u)
            {
                _state = ZeroSeedFallback;
            }
        }

        public uint State => _state;

        public uint NextUInt()
        {
            var value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return value;
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax), "Upper bound must be positive.");
            }

            return (int)(NextUInt() % (uint)exclusiveMax);
        }

        public float NextFloat()
        {
            // The upper 24 bits map exactly into a single-precision [0, 1) value.
            return (NextUInt() >> 8) * (1f / 16777216f);
        }
    }
}
