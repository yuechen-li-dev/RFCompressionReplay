namespace RfCompressionReplay.Core.Randomness;

public sealed class SeededRandom : ISeededRandom
{
    private readonly Random _random;

    public SeededRandom(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    public int Seed { get; }

    public double NextDouble() => _random.NextDouble();

    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
}
