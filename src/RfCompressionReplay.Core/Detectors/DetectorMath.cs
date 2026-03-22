namespace RfCompressionReplay.Core.Detectors;

internal static class DetectorMath
{
    public static double RoundScore(double value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
