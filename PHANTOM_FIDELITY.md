# Phantom Misses presentation and verification

This document describes the presentation fixes prepared for the 1.0.1 stability release. Development took place on `fix/phantom-miss-fidelity` in this personal fork. The [latest release](https://github.com/yyan115/osu/releases/latest) provides Windows and Linux packages.

## Presentation changes

Successful phantom targets keep their real hit judgement and input timing, but remain visually idle until the normal successful hit window expires. At that point they use the native circle MISS state, skin callbacks, judgement renderer, and combo-break sample. Successful-hit flash, shrink/explosion and approach-circle removal are prevented before they reach skin callbacks, instead of being erased after they happen.

Premature genuine circle misses use the same delayed presentation. Scoring still records their real miss and original timing immediately. Native combo-break audio is suppressed for this mod so it cannot reveal an early genuine miss. Both kinds of displayed circle miss trigger the same skinnable combo-break sound path.

Sliders and spinners are not phantom targets. Normal authoritative score, combo, accuracy, and results are preserved. This is an unofficial local-practice client. Target frequency, spacing, warmup and seed behaviour have not been redesigned in this release.

## Regression coverage

The skin fixture creates paired native/unhit and successfully-hit phantom circles under each of the existing six skin configurations: Argon, Triangles, default legacy, Retro, metrics legacy test assets, and special legacy test assets with extrapolated animations. It compares component types, position, opacity, scale, size, colour, and deterministic rotation at controlled gameplay-clock times, including the inclusive hit-window boundary, the first timeout frame, and mid-fade. Native legacy MISS randomises its rotation on every playback. For that component, the comparison verifies transform times and normalised angular motion rather than demanding the same random angle.

Cases include early/on-time/late successful clicks, several OD and clock-rate values, hit animations/lighting on and off, Hidden and Classic callbacks, early genuine misses, drawable reuse, rewind between actual hit and visible miss, and live skin component reload before and after the miss. The paired tests do not replace the full-player tests, which also check real scoring, HUD concealment, mixed genuine/phantom misses, and target selection.

The configured rate cases exercise timestamp calculations using a manual clock; they are not a measurement of wall-clock audiovisual synchronisation. These tests compare drawable state rather than captured GPU pixels or audio waveforms. They do not establish human perceptual indistinguishability on every skin or hardware configuration. Actual misaims, recognisable slider/spinner mistakes and the unchanged warmup/spacing rules can still reveal a broken FC.

## Builds and stability checks

The desktop workflow runs native Windows/Linux builds, focused Phantom tests, mod validity, and broader regressions in both threading modes. These include HUD dependency loading, socket shutdown, imports, background processing, leaderboard/results, and Taiko judgement/replay/sample coverage. The separate full CI retains its configured Windows/Linux shards, code quality and mobile build checks.

Passing desktop runs produce portable packages with actual `.trx` reports, `COMMIT.txt`, `PHANTOM_VERSION`, and `BUILD-CHANNEL.txt`. Release publishing is restricted to `master`; development branches only produce preview artifacts. Extract each version into a separate writable folder and keep the old portable folder as a backup.
