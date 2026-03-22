namespace RfCompressionReplay.Core.Randomness;

public static class SeedMath
{
    public static int Combine(int seed, params int[] values)
    {
        unchecked
        {
            var combined = seed == 0 ? 17 : seed;
            foreach (var value in values)
            {
                combined = (combined * 31) + value;
            }

            return combined == int.MinValue ? int.MaxValue : Math.Abs(combined);
        }
    }
}
