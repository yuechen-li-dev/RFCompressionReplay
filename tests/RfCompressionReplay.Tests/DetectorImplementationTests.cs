using RfCompressionReplay.Core.Compression;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Signals;

namespace RfCompressionReplay.Tests;

public sealed class DetectorImplementationTests
{
    [Fact]
    public void EnergyDetectorScoresHigherEnergyInputHigher()
    {
        var detector = new EnergyDetector();
        var config = new DetectorConfig(DetectorCatalog.EnergyDetectorName, 0d, DetectorCatalog.EnergyDetectorMode);

        var lowEnergy = detector.Evaluate(new DetectorInput(0, [CreateWindow([0.1, 0.2, 0.1, 0.2])]), config);
        var highEnergy = detector.Evaluate(new DetectorInput(0, [CreateWindow([1.0, 1.1, 0.9, 1.2])]), config);
        var repeat = detector.Evaluate(new DetectorInput(0, [CreateWindow([1.0, 1.1, 0.9, 1.2])]), config);

        Assert.True(highEnergy.Score > lowEnergy.Score);
        Assert.Equal(highEnergy.Score, repeat.Score);
        Assert.Equal(highEnergy.IsAboveThreshold, repeat.IsAboveThreshold);
        Assert.Equal(highEnergy.Metrics["averageEnergy"], repeat.Metrics["averageEnergy"]);
    }

    [Fact]
    public void CovarianceAbsoluteValueDetectorScoresCorrelatedInputHigherThanIidLikeInput()
    {
        var detector = new CovarianceAbsoluteValueDetector();
        var config = new DetectorConfig(DetectorCatalog.CovarianceAbsoluteValueDetectorName, 0d, DetectorCatalog.CovarianceAbsoluteValueDetectorMode);

        var correlated = detector.Evaluate(new DetectorInput(0, [CreateWindow([1.0, 0.95, 1.05, 1.02, 0.98, 1.01, 0.99])]), config);
        var iidLike = detector.Evaluate(new DetectorInput(0, [CreateWindow([-0.69, -0.45, -0.59, -0.2, -0.6, -0.88, -0.65])]), config);
        var repeat = detector.Evaluate(new DetectorInput(0, [CreateWindow([1.0, 0.95, 1.05, 1.02, 0.98, 1.01, 0.99])]), config);

        Assert.True(correlated.Score > iidLike.Score);
        Assert.Equal(correlated.Score, repeat.Score);
        Assert.Equal(correlated.IsAboveThreshold, repeat.IsAboveThreshold);
        Assert.Equal(correlated.Metrics["lag1AbsoluteAutocovariance"], repeat.Metrics["lag1AbsoluteAutocovariance"]);
    }

    [Fact]
    public void LzmsaSerializationIsStableAndLittleEndian()
    {
        var serializer = new LzmsaWindowSerializer();
        var windows = new[] { CreateWindow([1.0, -2.5, 0.125]) };

        var bytesA = serializer.Serialize(windows);
        var bytesB = serializer.Serialize(windows);

        Assert.Equal(bytesA, bytesB);
        Assert.Equal(new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0xC0,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x3F,
        }, bytesA);
    }

    [Fact]
    public void LzmsaPaperScoreIsDeterministicAndDistinguishesFixtures()
    {
        var detector = new LzmsaPaperDetector(new LzmsaWindowSerializer(), new BrotliCompressionCodec());
        var config = new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 0d, DetectorCatalog.LzmsaPaperDetectorMode);

        var repetitive = detector.Evaluate(new DetectorInput(0, [CreateWindow(Enumerable.Repeat(0.25d, 64).ToArray())]), config);
        var randomLooking = detector.Evaluate(new DetectorInput(0, [CreateWindow(new[]
        {
            0.134921, -0.882341, 1.221994, -1.550201, 0.443211, -0.091231, 0.780112, -0.665423,
            1.002341, -1.118822, 0.223344, 0.998877, -0.334455, 0.556677, -0.778899, 1.101112,
        })]), config);
        var repeat = detector.Evaluate(new DetectorInput(0, [CreateWindow(Enumerable.Repeat(0.25d, 64).ToArray())]), config);

        Assert.Equal(repetitive.Score, repeat.Score);
        Assert.Equal(repetitive.IsAboveThreshold, repeat.IsAboveThreshold);
        Assert.Equal(repetitive.Metrics["compressedByteSum"], repeat.Metrics["compressedByteSum"]);
        Assert.NotEqual(repetitive.Score, randomLooking.Score);
    }

    private static SignalWindow CreateWindow(IReadOnlyList<double> samples)
    {
        return new SignalWindow(0, 0, samples);
    }
}
