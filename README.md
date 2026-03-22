# RfCompressionReplay

`RfCompressionReplay` is a .NET 8 experiment harness for an independent reproduction of a 2018 RF spectrum-sensing paper. M1 upgrades the M0 execution kernel from placeholder scoring to real, deterministic detector implementations over the existing synthetic signal path.

## What M1 Adds

- Real detector implementations for:
  - `ed`: average signal energy over the flattened sample window.
  - `cav`: lag-1 absolute autocovariance over the flattened sample window.
  - `lzmsa-paper`: the paper statistic contract, implemented as **serialize window -> compress bytes -> sum compressed output byte values**.
- Explicit detector identifier and mode validation.
- An explicit, tested serialization contract for compression-backed scoring.
- Per-trial artifacts that now record detector name, detector mode, and score.
- Focused xUnit coverage for detector behavior, serialization stability, config validation, and end-to-end execution.

## Important LZMSA Caveat

The `lzmsa-paper` mode is named for the paper's **byte-sum-over-compressed-bytes statistic**. In this harness, that statistic is **not** treated as compressed length or compression ratio.

For M1, the harness preserves the paper-style score contract while using a deterministic built-in .NET compression backend (`BrotliStream`) behind an explicit abstraction. That means the current implementation is a provisional compression substitution for true LZMA, but the score that the harness reports is still the byte sum of the compressed output stream for the configured backend.

## Repository Layout

- `src/RfCompressionReplay.Core/`: typed config, execution flow, detector implementations, serialization/compression plumbing, and artifact writing.
- `src/RfCompressionReplay.Cli/`: command-line entry point for running a config.
- `tests/RfCompressionReplay.Tests/`: xUnit tests covering config validation, detector behavior, determinism, artifact writing, and end-to-end execution.
- `configs/m1.ed.json`, `configs/m1.cav.json`, `configs/m1.lzmsa-paper.json`: runnable sample M1 configurations.
- `docs/DETECTOR_IMPLEMENTATION_NOTES.md`: exact M1 detector formulas, serialization contract, and fidelity caveats.
- `docs/REPRODUCTION_SCOPE.md`: concise statement of the reproduction scope.

## Running the M1 Sample Experiments

1. Install the .NET 8 SDK.
2. From the repository root, run one of:

```bash
dotnet run --project src/RfCompressionReplay.Cli -- configs/m1.ed.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m1.cav.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m1.lzmsa-paper.json
```

On success, the CLI prints the run identifier and the artifact directory.

## Produced Artifacts

Each run writes a deterministic per-run folder beneath the configured output root:

- `manifest.json`: run metadata, environment summary, config path, scenario name, warnings, and artifact list.
- `summary.json`: aggregate summary including detector name, detector mode, trial count, min/max/mean score, and above-threshold count.
- `trials.csv`: per-trial detector name, detector mode, score, threshold decision, and simple sample-window context.

If a same-second rerun would collide, the harness appends a readable suffix such as `_2` to keep artifacts isolated.

## M1 Deliberate Simplifications

- The signal path is still a deterministic synthetic stub; M1 improves detector truth, not RF realism.
- The CAV implementation is intentionally modest: it is a documented lag-1 absolute autocovariance statistic, not a claim of full paper-grade covariance processing.
- The `lzmsa-paper` score currently uses deterministic Brotli compression rather than a full LZMA implementation, but it keeps the byte-sum-over-compressed-bytes contract explicit and swappable.
- No ROC/AUC sweeps, LTE generation, SDR ingestion, notebooks, or plotting layers are added in M1.

## What Later Milestones Can Add

- A truer LZMA backend behind the existing compression abstraction.
- Additional statistic ablations such as compressed length, once deliberately introduced.
- Richer synthetic or external dataset providers.
- Broader evaluation workflows, including ROC/AUC and figure reproduction work.
