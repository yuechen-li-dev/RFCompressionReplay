# M6a1 Usefulness-Mapping Findings

## Scope

- Task families: `structured-burst-vs-noise-only`, `colored-nuisance-vs-white-noise`, `equal-energy-structured-vs-unstructured`
- Detector panel: `ed`, `cav`, `lzmsa-paper`, `lzmsa-rms-normalized-mean-compressed-byte-value`
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Seeds: 86420, 97531, 24680
- Trial count per class per seed/condition: 6
- Retention mode used: `smoke` (with only manifest + M6a1 summary artifacts retained at the top level for this milestone)
- Config provenance: `m6a1-usefulness-mapping-smoke` / `m6a1-smoke`

## Task-Family Read

### `structured-burst-vs-noise-only`

- Best overall by median AUC: `cav` at 0.625 median AUC with 6/18 best-or-tied-best condition wins.
- Best baseline: `cav` at 0.625 median AUC.
- Best compression-derived detector: `lzmsa-paper` at 0.528 median AUC.
- RMS-normalized mean vs `lzmsa-paper`: normalized mean median AUC 0.500 vs paper 0.528; median pairwise AUC gap (normalized minus paper) 0.042.
- Cautious read: ED/CAV remained stronger here, and lzmsa-paper kept some measurable edge over the normalized-mean proxy.

### `colored-nuisance-vs-white-noise`

- Best overall by median AUC: `ed` at 1.000 median AUC with 16/18 best-or-tied-best condition wins.
- Best baseline: `ed` at 1.000 median AUC.
- Best compression-derived detector: `lzmsa-paper` at 0.625 median AUC.
- RMS-normalized mean vs `lzmsa-paper`: normalized mean median AUC 0.458 vs paper 0.625; median pairwise AUC gap (normalized minus paper) -0.083.
- Cautious read: ED/CAV remained stronger here, and lzmsa-paper kept some measurable edge over the normalized-mean proxy.

### `equal-energy-structured-vs-unstructured`

- Best overall by median AUC: `cav` at 1.000 median AUC with 15/18 best-or-tied-best condition wins.
- Best baseline: `cav` at 1.000 median AUC.
- Best compression-derived detector: `lzmsa-rms-normalized-mean-compressed-byte-value` at 0.556 median AUC.
- RMS-normalized mean vs `lzmsa-paper`: normalized mean median AUC 0.556 vs paper 0.500; median pairwise AUC gap (normalized minus paper) 0.083.
- Cautious read: ED/CAV remained at least as strong on median AUC, while the normalized-mean proxy stayed close to lzmsa-paper as the simpler compression summary.

## Practical Candidate Read

- RMS-normalized mean compressed byte value reached the highest compression-family median AUC on 1 of 3 task families in this suite.
- Against the best baseline within each task family, its median AUC gap ranged from -0.500 to -0.153.
- Cautious practical read: it showed some value, but lzmsa-paper still bought a noticeable edge on part of the tested suite.

## Overall Conclusion

- Within this synthetic task suite, ED/CAV covered most of the detectable separation, while the compression-derived detectors mainly served as a lightweight secondary view rather than a dominant replacement.

## Caveats

- This remains a synthetic-only benchmark suite.
- The structured tasks use simple OFDM-like / correlated-process constructions, not LTE-faithful channels or captures.
- The current deterministic compression-backend caveat remains in force.
- No SDR-facing or deployment-readiness claim is made here.
- M6a1 is usefulness mapping inside the current harness, not final validation.
