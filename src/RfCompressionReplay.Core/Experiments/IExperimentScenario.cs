using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Models;
using RfCompressionReplay.Core.Randomness;

namespace RfCompressionReplay.Core.Experiments;

public interface IExperimentScenario
{
    string Name { get; }
    ExperimentResult Execute(ExperimentConfig config, ISeededRandom random);
}
