namespace GenericGachaRPG
{
    public interface IRandomService
    {
        int Seed { get; }
        int NextInt(int minInclusive, int maxExclusive);
        float NextFloat();
        double NextDouble();
        void Reset(int seed);
    }
}
