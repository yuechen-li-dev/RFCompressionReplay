# RfCompressionReplay

`RfCompressionReplay` is a .NET 8 experiment harness for an independent reproduction of a 2018 RF spectrum-sensing paper. M0 focuses on a typed, deterministic execution kernel and reproducibility plumbing rather than paper-faithful detector math.

## What M0 Includes

- A strongly typed JSON configuration model for experiment runs.
- Explicit configuration validation with clear failure messages.
- A single seeded randomness abstraction for deterministic execution.
- A dummy scenario, dummy signal provider, and placeholder detector contract for end-to-end execution.
- Per-run artifact writing for `manifest.json`, `summary.json`, and `trials.csv`.
- A small CLI that loads config, validates it, runs the experiment, and reports the artifact directory.
- Focused xUnit coverage for config, validation, deterministic execution, artifact writing, and the application/CLI path.

## Repository Layout

- `src/RfCompressionReplay.Core/`: contracts, execution flow, artifacts, and placeholder components.
- `src/RfCompressionReplay.Cli/`: command-line entry point for running a config.
- `tests/RfCompressionReplay.Tests/`: xUnit tests covering M0 behavior.
- `configs/m0.dummy.json`: runnable sample configuration.
- `docs/REPRODUCTION_SCOPE.md`: concise statement of the reproduction scope.

## Running the Dummy Experiment

1. Install the .NET 8 SDK.
2. From the repository root, run:

```bash
dotnet run --project src/RfCompressionReplay.Cli -- configs/m0.dummy.json
```

On success, the CLI prints the run identifier and the artifact directory.

## Produced Artifacts

Each run writes a deterministic per-run folder beneath the configured output root:

- `manifest.json`: run metadata, environment summary, config path, scenario name, warnings, and artifact list.
- `summary.json`: typed aggregate summary for the run.
- `trials.csv`: per-trial dummy measurements and placeholder detector scores.

The sample config writes beneath the config file directory under `configs/artifacts/<timestamp>_m0-dummy_seed12345/`. If a same-second rerun would collide, the harness appends a readable suffix such as `_2` to keep artifacts isolated.

## M0 Deliberate Simplifications

- Detector behavior is placeholder-only; no ED, CAV, LZMSA, ROC, or AUC logic is implemented yet.
- Signal generation is a deterministic synthetic stub suitable only for plumbing and tests.
- Artifact writing is intentionally CSV/JSON-first and dependency-light.
- Git commit capture is best-effort and falls back to `unknown` when unavailable.

## What Later Milestones Can Add

- Real detector implementations in `src/RfCompressionReplay.Core/Detectors/`.
- Synthetic or external dataset providers in `src/RfCompressionReplay.Core/Signals/`.
- Richer scenario types, evaluation metrics, and analysis workflows.
- Additional manifest provenance and result-comparison tooling.
