using RfCompressionReplay.Core.Compression;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;
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
    public void LzmsaRepresentationPerturbationsApplyScalingAndFloat32PackingDeterministically()
    {
        var windows = new[] { CreateWindow([1.0, -2.5, 0.125]) };

        var scaledSerializer = new LzmsaWindowSerializer(new RepresentationConfig(0.5d, RepresentationFormats.Float64LittleEndian));
        var float32Serializer = new LzmsaWindowSerializer(new RepresentationConfig(1d, RepresentationFormats.Float32LittleEndian));

        var scaledBytes = scaledSerializer.Serialize(windows);
        var float32Bytes = float32Serializer.Serialize(windows);

        Assert.Equal(new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x3F,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF4, 0xBF,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB0, 0x3F,
        }, scaledBytes);
        Assert.Equal(new byte[]
        {
            0x00, 0x00, 0x80, 0x3F,
            0x00, 0x00, 0x20, 0xC0,
            0x00, 0x00, 0x00, 0x3E,
        }, float32Bytes);
    }

    [Fact]
    public void LzmsaRmsNormalizationCancelsPureScaleDifferencesDeterministically()
    {
        var windows = new[] { CreateWindow([1.0, -2.5, 0.125]) };

        var normalizedBaseline = new LzmsaWindowSerializer(new RepresentationConfig(
            1d,
            RepresentationFormats.Float64LittleEndian,
            RepresentationNormalizations.Rms,
            1d));
        var normalizedScaled = new LzmsaWindowSerializer(new RepresentationConfig(
            2d,
            RepresentationFormats.Float64LittleEndian,
            RepresentationNormalizations.Rms,
            1d));

        var baselineBytes = normalizedBaseline.Serialize(windows);
        var scaledBytes = normalizedScaled.Serialize(windows);

        Assert.Equal(baselineBytes, scaledBytes);
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

    [Fact]
    public void LzmsaScoreVariantsExposeDistinctScoresForSameCompressedPayload()
    {
        var input = new DetectorInput(0, [
            CreateWindow(Enumerable.Repeat(0.25d, 64).ToArray()),
            CreateWindow(new[]
            {
                0.125, 0.375, 0.625, 0.875, -0.125, -0.375, -0.625, -0.875,
                1.125, 1.375, 1.625, 1.875, -1.125, -1.375, -1.625, -1.875,
            })]);

        var paper = DetectorFactory.Create(new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 0d, DetectorCatalog.LzmsaPaperDetectorMode))
            .Evaluate(input, new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 0d, DetectorCatalog.LzmsaPaperDetectorMode));
        var compressedLength = DetectorFactory.Create(new DetectorConfig(DetectorCatalog.LzmsaCompressedLengthDetectorName, 0d, DetectorCatalog.LzmsaCompressedLengthDetectorMode))
            .Evaluate(input, new DetectorConfig(DetectorCatalog.LzmsaCompressedLengthDetectorName, 0d, DetectorCatalog.LzmsaCompressedLengthDetectorMode));
        var normalized = DetectorFactory.Create(new DetectorConfig(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, 0d, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode))
            .Evaluate(input, new DetectorConfig(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, 0d, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode));
        var mean = DetectorFactory.Create(new DetectorConfig(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, 0d, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode))
            .Evaluate(input, new DetectorConfig(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, 0d, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode));

        Assert.Equal(paper.Metrics["compressedByteSum"], paper.Score);
        Assert.Equal(compressedLength.Metrics["compressedByteCount"], compressedLength.Score);
        Assert.Equal(
            compressedLength.Metrics["compressedByteCount"] / normalized.Metrics["inputByteCount"],
            normalized.Score,
            precision: 12);
        Assert.Equal(
            paper.Metrics["compressedByteSum"] / paper.Metrics["compressedByteCount"],
            mean.Score,
            precision: 12);

        Assert.NotEqual(paper.Score, compressedLength.Score);
        Assert.NotEqual(paper.Score, normalized.Score);
        Assert.NotEqual(paper.Score, mean.Score);
        Assert.NotEqual(compressedLength.Score, normalized.Score);
    }

    [Fact]
    public void LzmsaScoreVariantsShareSerializationAndCompressionBasis()
    {
        var input = new DetectorInput(0, [
            CreateWindow(Enumerable.Repeat(0.5d, 32).ToArray()),
            CreateWindow(Enumerable.Range(0, 32).Select(index => index / 32d).ToArray())]);

        var paper = DetectorFactory.Create(new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 0d, DetectorCatalog.LzmsaPaperDetectorMode))
            .Evaluate(input, new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 0d, DetectorCatalog.LzmsaPaperDetectorMode));
        var compressedLength = DetectorFactory.Create(new DetectorConfig(DetectorCatalog.LzmsaCompressedLengthDetectorName, 0d, DetectorCatalog.LzmsaCompressedLengthDetectorMode))
            .Evaluate(input, new DetectorConfig(DetectorCatalog.LzmsaCompressedLengthDetectorName, 0d, DetectorCatalog.LzmsaCompressedLengthDetectorMode));
        var normalized = DetectorFactory.Create(new DetectorConfig(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, 0d, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode))
            .Evaluate(input, new DetectorConfig(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, 0d, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode));
        var mean = DetectorFactory.Create(new DetectorConfig(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, 0d, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode))
            .Evaluate(input, new DetectorConfig(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, 0d, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode));

        foreach (var metricName in new[] { "serializedByteCount", "inputByteCount", "compressedByteCount", "compressedByteSum" })
        {
            Assert.Equal(paper.Metrics[metricName], compressedLength.Metrics[metricName]);
            Assert.Equal(paper.Metrics[metricName], normalized.Metrics[metricName]);
            Assert.Equal(paper.Metrics[metricName], mean.Metrics[metricName]);
        }
    }

    [Fact]
    public void LzmsaMeanCompressedByteValueUsesExplicitHigherScoreOrientation()
    {
        Assert.Equal(
            ScoreOrientation.HigherScoreMorePositive,
            DetectorCatalog.GetScoreOrientation(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName));
    }

    [Fact]
    public void LzmsaRmsNormalizedMeanCompressedByteValueUsesExplicitHigherScoreOrientation()
    {
        Assert.Equal(
            ScoreOrientation.HigherScoreMorePositive,
            DetectorCatalog.GetScoreOrientation(DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName));
    }

    [Fact]
    public void LzmsaMeanCompressedByteValueUsesHigherScoreThresholdSemantics()
    {
        var input = new DetectorInput(0, [CreateWindow(Enumerable.Repeat(0.25d, 64).ToArray())]);
        var config = new DetectorConfig(
            Name: DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
            Threshold: 100d,
            Mode: DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode);

        var result = DetectorFactory.Create(config).Evaluate(input, config);

        Assert.Equal(result.Score >= config.Threshold, result.IsAboveThreshold);
    }

    [Fact]
    public void LzmsaRmsNormalizedMeanCompressedByteValueIgnoresPureScaleDifferences()
    {
        var baselineInput = new DetectorInput(0, [CreateWindow([1.0, -2.5, 0.125, 0.75])]);
        var scaledInput = new DetectorInput(0, [CreateWindow([2.0, -5.0, 0.25, 1.5])]);
        var config = new DetectorConfig(
            Name: DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
            Threshold: 100d,
            Mode: DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorMode);

        var detector = DetectorFactory.Create(config);
        var baseline = detector.Evaluate(baselineInput, config);
        var scaled = detector.Evaluate(scaledInput, config);

        Assert.Equal(baseline.Score, scaled.Score);
        Assert.Equal(baseline.Metrics["compressedByteCount"], scaled.Metrics["compressedByteCount"]);
    }


    [Fact]
    public void LzmsaLengthBasedVariantsUseLowerScoreThresholdSemantics()
    {
        var input = new DetectorInput(0, [CreateWindow(Enumerable.Repeat(0.25d, 64).ToArray())]);
        var compressedLengthConfig = new DetectorConfig(
            Name: DetectorCatalog.LzmsaCompressedLengthDetectorName,
            Threshold: 32d,
            Mode: DetectorCatalog.LzmsaCompressedLengthDetectorMode);
        var normalizedConfig = new DetectorConfig(
            Name: DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName,
            Threshold: 0.1d,
            Mode: DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode);

        var compressedLength = DetectorFactory.Create(compressedLengthConfig).Evaluate(input, compressedLengthConfig);
        var normalized = DetectorFactory.Create(normalizedConfig).Evaluate(input, normalizedConfig);

        Assert.Equal(compressedLength.Score <= compressedLengthConfig.Threshold, compressedLength.IsAboveThreshold);
        Assert.Equal(normalized.Score <= normalizedConfig.Threshold, normalized.IsAboveThreshold);
    }

    private static SignalWindow CreateWindow(IReadOnlyList<double> samples)
    {
        return new SignalWindow(0, 0, samples);
    }
}
