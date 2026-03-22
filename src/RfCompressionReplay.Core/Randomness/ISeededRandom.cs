namespace RfCompressionReplay.Core.Randomness;

public interface ISeededRandom
{
    int Seed { get; }
    double NextDouble();
    int Next(int minValue, int maxValue);
}
