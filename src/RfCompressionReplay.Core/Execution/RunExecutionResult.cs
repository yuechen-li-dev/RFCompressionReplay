using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Execution;

public sealed record RunExecutionResult(
    RunManifest Manifest,
    ExperimentResult Result,
    string RunDirectory);
