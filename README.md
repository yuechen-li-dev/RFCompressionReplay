# RfCompressionReplay

`RfCompressionReplay` is a .NET 8 experiment harness for an independent reproduction of a 2018 RF spectrum-sensing paper. M2 extends the typed M0/M1 execution harness with deterministic synthetic benchmark generation so the current detectors can be compared on controlled signal-present versus noise-only tasks before any later ROC/AUC or figure-reproduction work.

## What M2 Adds

- Seeded Gaussian noise generation with explicit mean and standard deviation controls.
- A documented Gaussian-emitter control implemented as an independent Gaussian signal process mixed into Gaussian background noise at a configured target SNR.
- A modest OFDM-like structured source built from seeded multi-tone subcarriers with seeded BPSK-like symbol changes over fixed symbol intervals.
- Deterministic finite-stream SNR mixing using an explicit power-ratio definition.
- Deterministic consecutive-window trial extraction over pre-generated base streams.
- Monte Carlo execution over one or more synthetic cases per run.
- Per-trial artifacts that now record scenario metadata, target label, source type, SNR, window length, start index, detector name, detector mode, and detector score.
- Summary artifacts that group detector scores by scenario and class label with count, min, max, mean, standard deviation, and above-threshold totals.

## Scientific Intent of M2

M2 is the first synthetic benchmark scaffold for detector comparison, not a claim of paper-figure reproduction.

The current benchmark cases are designed to answer two narrow questions:

1. **Signal present vs noise only:** does a detector respond differently when a synthetic signal is mixed into Gaussian noise?
2. **Gaussian-emitter control vs Gaussian noise:** does a detector react only to energy/power differences, or does it also respond differently when temporal structure is present?

That second control matters because a pure energy detector should respond strongly when total power increases, while a structure-sensitive detector may respond differently to the OFDM-like source than it does to a Gaussian-emitter control with similar energy conditions.

## Synthetic Scenarios Supported in M2

### 1. Noise-only baseline

A seeded Gaussian noise source produces a real-valued base stream with configurable:

- sample count via `benchmark.baseStreamLength`
- mean via `benchmark.noise.mean`
- standard deviation via `benchmark.noise.standardDeviation`

### 2. Gaussian-emitter control

In this repository, the Gaussian-emitter control means:

- generate an independent Gaussian signal process with its own mean and standard deviation
- generate independent Gaussian background noise
- scale the signal process so the finite generated stream meets the requested target SNR against that noise stream
- add the scaled signal and background noise sample-by-sample

This keeps the control intentionally close to “energy/power difference without rich temporal structure.” It is not intended to model a real communications waveform.

### 3. OFDM-like structured signal

The OFDM-like source is a deterministic, real-valued approximation with:

- multiple subcarrier tones
- seeded symbol values per subcarrier and symbol interval
- configurable subcarrier count, carrier spacing, symbol length, amplitude, and symbol seed
- synthesis into a real-valued time series by summing cosine subcarriers

Important caveat: **this is not LTE and is not claimed to be standards-faithful OFDM.** It is only a modest structured synthetic control with more temporal organization than the Gaussian-emitter baseline.

## SNR Definition Used in M2

For synthetic cases with a signal component, M2 defines SNR over the finite generated streams as:

`SNR_dB = 10 * log10(P_signal / P_noise)`

where:

- `P_signal` is the average power of the clean generated signal stream before addition
- `P_noise` is the average power of the independently generated Gaussian noise stream

The harness computes those finite-stream powers, scales the clean signal to the requested target SNR, and then forms:

`mixed[n] = scaledSignal[n] + noise[n]`

So the configured SNR is explicit, deterministic, and tied to the exact finite streams used for the run.

## Windowing and Monte Carlo Trial Generation

M2 uses an explicit consecutive-window approach:

1. Pre-generate one longer synthetic base stream per configured case.
2. Draw deterministic seeded start indices.
3. For each trial, extract `scenario.sampleWindowCount` consecutive windows of length `scenario.samplesPerWindow`.
4. Flatten those windows through the existing detector path.

There is no silent switch to scattered random sample picks. The windowing policy is intentionally simple and auditable.

## Produced Artifacts

Each run writes a deterministic per-run folder beneath the configured output root:

- `manifest.json`: run metadata, environment summary, config path, scenario name, warnings, and artifact list.
- `summary.json`: grouped detector score statistics by scenario and target label.
- `trials.csv`: per-trial detector scores with scenario metadata, label, source type, SNR, window length, and sampled start index.

If a same-second rerun would collide, the harness appends a readable suffix such as `_2` to keep artifacts isolated.

## Repository Layout

- `src/RfCompressionReplay.Core/`: typed config, execution flow, detector implementations, synthetic generators, window sampling, SNR mixing, and artifact writing.
- `src/RfCompressionReplay.Cli/`: command-line entry point for running a config.
- `tests/RfCompressionReplay.Tests/`: xUnit tests covering config validation, detector behavior, synthetic generator determinism, windowing, SNR sanity, and end-to-end execution.
- `configs/m2.noise-only.ed.json`, `configs/m2.gaussian-emitter.ed.json`, `configs/m2.ofdm-like.cav.json`, `configs/m2.mixed.lzmsa-paper.json`: runnable sample M2 configurations.
- `docs/M2_SYNTHETIC_BENCHMARKS.md`: M2 benchmark definitions, caveats, SNR formula, and windowing approach.
- `docs/DETECTOR_IMPLEMENTATION_NOTES.md`: exact detector formulas and implementation notes from M1.
- `docs/REPRODUCTION_SCOPE.md`: concise statement of the reproduction scope.

## Running the M2 Sample Experiments

1. Install the .NET 8 SDK.
2. From the repository root, run one of:

```bash
dotnet run --project src/RfCompressionReplay.Cli -- configs/m2.noise-only.ed.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m2.gaussian-emitter.ed.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m2.ofdm-like.cav.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m2.mixed.lzmsa-paper.json
```

On success, the CLI prints the run identifier and the artifact directory.

## Deliberate M2 Simplifications

- The OFDM-like source is a structured synthetic control, not LTE.
- The Gaussian-emitter control is intentionally simple and Gaussian-like rather than physically realistic.
- M2 does not claim paper-figure reproduction yet.
- M2 does not add SDR ingestion, external datasets, plotting libraries, notebooks, or a full ROC/AUC sweep framework.
- The existing detector implementations remain modest, explicit statistics intended to support later controlled comparisons.

## What Later Milestones Can Add

- Broader SNR sweeps and threshold sweeps for ROC/AUC work.
- Richer structured signal families or external datasets.
- More standards-faithful waveform generation if later justified.
- Figure-reproduction workflows once synthetic benchmark behavior is established.
