# M6a2 Complementary-Value Usefulness Mapping

M6a2 is the second usefulness-mapping milestone in the synthetic .NET 8 harness.

## Purpose

M6a1 showed that compression-derived detectors were not dominant replacements for ED or CAV on the first compact application-style synthetic suite. M6a2 therefore asks two narrower synthetic questions:

1. Do compression-derived features become more competitive on a fairer task family where **both classes are non-iid** and have meaningful second-order structure?
2. Does the strongest simple compression-derived feature from the M5/M6a1 arc — **RMS-normalized mean compressed byte value** — add practical value as a **complementary feature** on top of a tiny `[ED, CAV]` bundle?

This remains usefulness mapping, not real-world validation.

## Task Families

M6a2 keeps the task suite intentionally small.

### 1. `engineered-structure-vs-natural-correlation`

- **Positive class:** a weak organized burst-OFDM-like process mixed into Gaussian noise at the configured SNR.
- **Negative class:** a correlated Gaussian nuisance process mixed to the same SNR.
- **Intent:** give the compression-derived family a fairer standalone test where both classes have structure and plain energy is not aligned with a signal-present vs white-noise boundary.

### 2. `equal-energy-engineered-structure-vs-natural-correlation`

- **Positive class:** an OFDM-like engineered structured process mixed to the configured SNR.
- **Negative class:** a correlated Gaussian nuisance process mixed to the same configured SNR.
- **Intent:** make the power scale explicit and reduce ED's default advantage so the benchmark is more about coarse structure than raw energy.

The equal-energy construction is still synthetic. It uses the harness's shared SNR mixer and is sanity-checked in tests to keep positive/negative average powers close at the benchmarked SNR.

## Detector Panel

Standalone detector comparison is intentionally limited to:

- `ed`
- `cav`
- `lzmsa-paper`
- `lzmsa-rms-normalized-mean-compressed-byte-value`

## Bundle Comparison

M6a2 adds a tiny explicit bundle readout rather than a larger ML framework.

- **Bundle A:** `[ED, CAV]`
- **Bundle B:** `[ED, CAV, RMS-normalized mean compressed byte value]`

The bundle readout is a deterministic leave-one-seed-out logistic regression trained **separately within each `(task family, SNR, window length)` condition** and evaluated only on the held-out seed for that same condition. This keeps the procedure auditable and avoids train/test leakage across seeds.

## Default Grid

Primary checked-in M6a2 config:

- SNRs: `-9`, `-3`, `0` dB
- Window lengths: `64`, `128`
- Seeds: `86420`, `97531`, `24680`
- Trials per class per seed/condition: `48`
- Retention mode: `milestone`

Smoke config keeps the same task families, detector panel, and bundle panel but reduces trials and uses `smoke` retention.

## Compact Artifacts

M6a2 uses summary-first retention by default. The checked-in run keeps only:

- `manifest.json`
- `m6a2_auc_comparison.csv`
- `m6a2_bundle_summary.csv`
- `m6a2_findings.md`

The checked-in primary run lives under:

- `configs/artifacts/m6a2/20260323T075441Z_m6a2-complementary-value-usefulness_seedpanel/`

## Current Artifact-Backed Read

In the checked-in M6a2 run:

- the compression-derived standalone pair became **more competitive** than they were in M6a1 on the new engineered-vs-correlated tasks,
- RMS-normalized mean compressed byte value was the stronger of the two compression-derived standalone summaries on both task families,
- adding that feature to `[ED, CAV]` gave a **modest median gain** on `engineered-structure-vs-natural-correlation`,
- but it did **not** improve the median bundle result on `equal-energy-engineered-structure-vs-natural-correlation`, even though it still helped in some conditions.

The cautious framing from this run is therefore: within this synthetic suite, the phenomenon looks **more useful as a complementary feature than as a universal replacement detector**.

## Caveats

- Synthetic-only benchmark.
- OFDM-like / burst constructions are not LTE-faithful.
- No SDR or captured-data claim is made.
- The current deterministic compression backend remains unchanged.
- M6a2 is still usefulness mapping, not deployment proof.
