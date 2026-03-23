using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Experiments;
using RfCompressionReplay.Core.Randomness;
using RfCompressionReplay.Core.Signals.Synthetic;

namespace RfCompressionReplay.Tests;

public sealed class SyntheticBenchmarkTests
{
    [Fact]
    public void GaussianNoiseGeneratorIsDeterministicForSameSeedAndConfig()
    {
        var generator = new GaussianNoiseGenerator();
        var config = new GaussianNoiseConfig(0d, 1d);

        var first = generator.Generate(16, config, new SeededRandom(123));
        var second = generator.Generate(16, config, new SeededRandom(123));

        Assert.Equal(first, second);
    }

    [Fact]
    public void GaussianEmitterControlShowsHigherEnergyThanNoiseOnlyWhenMixedAtZeroDb()
    {
        var builder = CreateStreamBuilder();
        var benchmark = new SyntheticBenchmarkConfig(
            BaseStreamLength: 2048,
            Noise: new GaussianNoiseConfig(0d, 1d),
            Cases: [TestConfigFactory.CreateGaussianEmitterCase(0d)]);

        var noiseOnly = builder.BuildStream(
            experimentSeed: 11,
            caseIndex: 0,
            benchmark,
            new SyntheticCaseConfig("noise-only", "noise-only", ExperimentConfigValidator.NoiseOnlySourceType, null, null, null));
        var gaussianEmitter = builder.BuildStream(11, 0, benchmark, TestConfigFactory.CreateGaussianEmitterCase(0d));

        Assert.True(SnrMixer.CalculateAveragePower(gaussianEmitter) > SnrMixer.CalculateAveragePower(noiseOnly));
    }

    [Fact]
    public void OfdmLikeGeneratorIsDeterministicAndRespondsToSymbolSeedChanges()
    {
        var generator = new OfdmLikeSignalGenerator();
        var config = new OfdmLikeSignalConfig(8, 32, 77, 0.75d, 1d);

        var first = generator.Generate(128, config, seed: 101);
        var second = generator.Generate(128, config, seed: 101);
        var changed = generator.Generate(128, config with { SymbolSeed = 78 }, seed: 101);

        Assert.Equal(first, second);
        Assert.NotEqual(first, changed);
    }

    [Fact]
    public void BurstOfdmLikeGeneratorProducesLocalizedStructuredBurst()
    {
        var generator = new BurstOfdmLikeGenerator(new OfdmLikeSignalGenerator());
        var config = new BurstOfdmLikeConfig(
            new OfdmLikeSignalConfig(8, 32, 91, 0.75d, 1d),
            StartFraction: 0.25d,
            LengthFraction: 0.2d);

        var samples = generator.Generate(100, config, seed: 55);

        Assert.All(samples.Take(25), sample => Assert.Equal(0d, sample));
        Assert.Contains(samples.Skip(25).Take(20), sample => Math.Abs(sample) > 1e-9);
        Assert.All(samples.Skip(45), sample => Assert.Equal(0d, sample));
    }

    [Fact]
    public void EqualEnergyTaskConstructionKeepsPositiveAndNegativeAveragePowerClose()
    {
        var builder = CreateStreamBuilder();
        var benchmark = new SyntheticBenchmarkConfig(
            BaseStreamLength: 16_384,
            Noise: new GaussianNoiseConfig(0d, 1d),
            Cases: Array.Empty<SyntheticCaseConfig>());
        var positive = TestConfigFactory.CreateEqualEnergyTask().PositiveCase with { SnrDb = -3d };
        var negative = TestConfigFactory.CreateEqualEnergyTask().NegativeCase with { SnrDb = -3d };

        var positiveStream = builder.BuildStream(41, 1, benchmark, positive);
        var negativeStream = builder.BuildStream(41, 0, benchmark, negative);
        var positivePower = SnrMixer.CalculateAveragePower(positiveStream);
        var negativePower = SnrMixer.CalculateAveragePower(negativeStream);

        Assert.InRange(Math.Abs(positivePower - negativePower), 0d, 0.15d);
    }

    [Fact]
    public void SnrMixerApproximatesRequestedSnrForFiniteStreams()
    {
        var noiseGenerator = new GaussianNoiseGenerator();
        var signalGenerator = new GaussianEmitterGenerator();
        var snrMixer = new SnrMixer();
        var noise = noiseGenerator.Generate(16_384, new GaussianNoiseConfig(0d, 1d), new SeededRandom(17));
        var signal = signalGenerator.Generate(16_384, new GaussianEmitterConfig(0d, 1d), new SeededRandom(29));

        var targetSnrDb = -3d;
        var mixed = snrMixer.MixToTargetSnr(signal, noise, targetSnrDb);
        var recoveredSignal = mixed.Zip(noise, (mixedSample, noiseSample) => mixedSample - noiseSample).ToArray();
        var actualSnrDb = SnrMixer.CalculateSnrDb(recoveredSignal, noise);

        Assert.InRange(actualSnrDb, targetSnrDb - 0.25d, targetSnrDb + 0.25d);
    }

    [Fact]
    public void ConsecutiveWindowSamplerIsDeterministic()
    {
        var sampler = new ConsecutiveWindowSampler();
        var stream = Enumerable.Range(0, 64).Select(index => (double)index).ToArray();

        var startsA = sampler.CreateStartIndices(stream.Length, 4, 2, 8, new SeededRandom(33));
        var startsB = sampler.CreateStartIndices(stream.Length, 4, 2, 8, new SeededRandom(33));
        var windowsA = sampler.ExtractWindows(stream, 0, startsA[0], 2, 8);
        var windowsB = sampler.ExtractWindows(stream, 0, startsB[0], 2, 8);

        Assert.Equal(startsA, startsB);
        Assert.Equal(windowsA.Count, windowsB.Count);
        for (var index = 0; index < windowsA.Count; index++)
        {
            Assert.Equal(windowsA[index].TrialIndex, windowsB[index].TrialIndex);
            Assert.Equal(windowsA[index].WindowIndex, windowsB[index].WindowIndex);
            Assert.Equal(windowsA[index].Samples, windowsB[index].Samples);
        }
    }

    [Fact]
    public void ControlShapeSanityShowsEnergyDetectorSeparatesEnergyControlWhileDetectorsDifferAcrossScenarios()
    {
        var config = TestConfigFactory.CreateSyntheticBenchmarkConfig(
            experimentId: "control-shape",
            detectorName: DetectorCatalog.EnergyDetectorName,
            detectorMode: DetectorCatalog.EnergyDetectorMode,
            threshold: 1.2d,
            cases:
            [
                new SyntheticCaseConfig("noise-only-baseline", "noise-only", ExperimentConfigValidator.NoiseOnlySourceType, null, null, null),
                TestConfigFactory.CreateGaussianEmitterCase(0d)
            ]);

        var energyScenario = new SyntheticBenchmarkScenario(CreateStreamBuilder(), new ConsecutiveWindowSampler(), new EnergyDetector());
        var energyResult = energyScenario.Execute(config, new SeededRandom(config.Seed));
        var noiseMean = energyResult.Summary.Groups.Single(group => group.ScenarioName == "noise-only-baseline").MeanScore;
        var emitterMean = energyResult.Summary.Groups.Single(group => group.ScenarioName == "gaussian-emitter-control").MeanScore;

        Assert.True(emitterMean > noiseMean);

        var comparativeConfig = TestConfigFactory.CreateSyntheticBenchmarkConfig(
            experimentId: "control-shape-comparison",
            detectorName: DetectorCatalog.CovarianceAbsoluteValueDetectorName,
            detectorMode: DetectorCatalog.CovarianceAbsoluteValueDetectorMode,
            threshold: 0d,
            cases:
            [
                TestConfigFactory.CreateGaussianEmitterCase(0d),
                TestConfigFactory.CreateOfdmLikeCase(-3d)
            ]);

        var cavScenario = new SyntheticBenchmarkScenario(CreateStreamBuilder(), new ConsecutiveWindowSampler(), new CovarianceAbsoluteValueDetector());
        var cavResult = cavScenario.Execute(comparativeConfig, new SeededRandom(comparativeConfig.Seed));
        var gaussianCav = cavResult.Summary.Groups.Single(group => group.ScenarioName == "gaussian-emitter-control").MeanScore;
        var ofdmCav = cavResult.Summary.Groups.Single(group => group.ScenarioName == "ofdm-like-signal").MeanScore;

        Assert.NotEqual(gaussianCav, ofdmCav);
    }

    private static SyntheticCaseStreamBuilder CreateStreamBuilder()
    {
        return new SyntheticCaseStreamBuilder(
            new GaussianNoiseGenerator(),
            new GaussianEmitterGenerator(),
            new OfdmLikeSignalGenerator(),
            new BurstOfdmLikeGenerator(new OfdmLikeSignalGenerator()),
            new CorrelatedGaussianProcessGenerator(),
            new SnrMixer());
    }
}
