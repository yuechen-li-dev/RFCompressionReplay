using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Detectors;

public interface IDetector
{
    string Name { get; }
    DetectorResult Evaluate(DetectorInput input, DetectorConfig config);
}
