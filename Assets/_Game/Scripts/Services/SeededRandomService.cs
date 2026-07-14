using System;

namespace GenericGachaRPG
{
    public sealed class SeededRandomService : IRandomService
    {
        private Random random;

        public int Seed { get; private set; }

        public SeededRandomService()
            : this(12345)
        {
        }

        public SeededRandomService(int seed)
        {
            Reset(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Maximum must not be less than minimum.");
            }

            return maxExclusive == minInclusive
                ? minInclusive
                : random.Next(minInclusive, maxExclusive);
        }

        public float NextFloat()
        {
            return (float)random.NextDouble();
        }

        public double NextDouble()
        {
            return random.NextDouble();
        }

        public void Reset(int seed)
        {
            Seed = seed;
            random = new Random(seed);
        }
    }
}
