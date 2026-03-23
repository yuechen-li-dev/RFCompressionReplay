using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Randomness;

namespace RfCompressionReplay.Core.Signals.Synthetic;

public sealed class SyntheticCaseStreamBuilder
{
    private readonly GaussianNoiseGenerator _noiseGenerator;
    private readonly GaussianEmitterGenerator _gaussianEmitterGenerator;
    private readonly OfdmLikeSignalGenerator _ofdmLikeSignalGenerator;
    private readonly BurstOfdmLikeGenerator _burstOfdmLikeGenerator;
    private readonly CorrelatedGaussianProcessGenerator _correlatedGaussianProcessGenerator;
    private readonly SnrMixer _snrMixer;

    public SyntheticCaseStreamBuilder(
        GaussianNoiseGenerator noiseGenerator,
        GaussianEmitterGenerator gaussianEmitterGenerator,
        OfdmLikeSignalGenerator ofdmLikeSignalGenerator,
        BurstOfdmLikeGenerator burstOfdmLikeGenerator,
        CorrelatedGaussianProcessGenerator correlatedGaussianProcessGenerator,
        SnrMixer snrMixer)
    {
        _noiseGenerator = noiseGenerator;
        _gaussianEmitterGenerator = gaussianEmitterGenerator;
        _ofdmLikeSignalGenerator = ofdmLikeSignalGenerator;
        _burstOfdmLikeGenerator = burstOfdmLikeGenerator;
        _correlatedGaussianProcessGenerator = correlatedGaussianProcessGenerator;
        _snrMixer = snrMixer;
    }

    public double[] BuildStream(int experimentSeed, int caseIndex, SyntheticBenchmarkConfig benchmark, SyntheticCaseConfig syntheticCase)
    {
        var noiseSeed = SeedMath.Combine(experimentSeed, 101, caseIndex);
        var noise = _noiseGenerator.Generate(benchmark.BaseStreamLength, benchmark.Noise, new SeededRandom(noiseSeed));

        if (string.Equals(syntheticCase.SourceType, ExperimentConfigValidator.NoiseOnlySourceType, StringComparison.OrdinalIgnoreCase))
        {
            return noise;
        }

        if (syntheticCase.SnrDb is null)
        {
            throw new InvalidOperationException($"Synthetic case '{syntheticCase.Name}' requires SnrDb for source type '{syntheticCase.SourceType}'.");
        }

        double[] signal = BuildSignal(experimentSeed, caseIndex, benchmark.BaseStreamLength, syntheticCase);

        return _snrMixer.MixToTargetSnr(signal, noise, syntheticCase.SnrDb.Value);
    }

    private double[] BuildSignal(int experimentSeed, int caseIndex, int sampleCount, SyntheticCaseConfig syntheticCase)
    {
        if (string.Equals(syntheticCase.SourceType, ExperimentConfigValidator.GaussianEmitterSourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildGaussianEmitter(experimentSeed, caseIndex, sampleCount, syntheticCase);
        }

        if (string.Equals(syntheticCase.SourceType, ExperimentConfigValidator.OfdmLikeSourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildOfdmLike(experimentSeed, caseIndex, sampleCount, syntheticCase);
        }

        if (string.Equals(syntheticCase.SourceType, ExperimentConfigValidator.BurstOfdmLikeSourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildBurstOfdmLike(experimentSeed, caseIndex, sampleCount, syntheticCase);
        }

        if (string.Equals(syntheticCase.SourceType, ExperimentConfigValidator.CorrelatedGaussianSourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildCorrelatedGaussian(experimentSeed, caseIndex, sampleCount, syntheticCase);
        }

        throw new InvalidOperationException($"Synthetic case '{syntheticCase.Name}' uses unsupported source type '{syntheticCase.SourceType}'.");
    }

    private double[] BuildGaussianEmitter(int experimentSeed, int caseIndex, int sampleCount, SyntheticCaseConfig syntheticCase)
    {
        if (syntheticCase.GaussianEmitter is null)
        {
            throw new InvalidOperationException($"Synthetic case '{syntheticCase.Name}' is missing GaussianEmitter parameters.");
        }

        var signalSeed = SeedMath.Combine(experimentSeed, 211, caseIndex);
        return _gaussianEmitterGenerator.Generate(sampleCount, syntheticCase.GaussianEmitter, new SeededRandom(signalSeed));
    }

    private double[] BuildOfdmLike(int experimentSeed, int caseIndex, int sampleCount, SyntheticCaseConfig syntheticCase)
    {
        if (syntheticCase.OfdmLike is null)
        {
            throw new InvalidOperationException($"Synthetic case '{syntheticCase.Name}' is missing OfdmLike parameters.");
        }

        var signalSeed = SeedMath.Combine(experimentSeed, 307, caseIndex, syntheticCase.OfdmLike.SymbolSeed);
        return _ofdmLikeSignalGenerator.Generate(sampleCount, syntheticCase.OfdmLike, signalSeed);
    }

    private double[] BuildBurstOfdmLike(int experimentSeed, int caseIndex, int sampleCount, SyntheticCaseConfig syntheticCase)
    {
        if (syntheticCase.BurstOfdmLike is null)
        {
            throw new InvalidOperationException($"Synthetic case '{syntheticCase.Name}' is missing BurstOfdmLike parameters.");
        }

        var signalSeed = SeedMath.Combine(experimentSeed, 401, caseIndex, syntheticCase.BurstOfdmLike.Carrier.SymbolSeed);
        return _burstOfdmLikeGenerator.Generate(sampleCount, syntheticCase.BurstOfdmLike, signalSeed);
    }

    private double[] BuildCorrelatedGaussian(int experimentSeed, int caseIndex, int sampleCount, SyntheticCaseConfig syntheticCase)
    {
        if (syntheticCase.CorrelatedGaussian is null)
        {
            throw new InvalidOperationException($"Synthetic case '{syntheticCase.Name}' is missing CorrelatedGaussian parameters.");
        }

        var signalSeed = SeedMath.Combine(experimentSeed, 503, caseIndex);
        return _correlatedGaussianProcessGenerator.Generate(sampleCount, syntheticCase.CorrelatedGaussian, new SeededRandom(signalSeed));
    }
}
