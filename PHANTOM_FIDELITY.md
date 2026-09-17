# Phantom Misses fidelity development

This work is isolated on `fix/phantom-miss-fidelity`. It has not been merged into `master` or published over `phantom-v1.0.0`.

## Presentation changes

Successful phantom targets keep their real hit judgement and input timing, but remain visually idle until the normal successful hit window expires. At that point they use the native circle MISS state, skin callbacks, judgement renderer, and combo-break sample. Successful-hit flash, shrink/explosion and approach-circle removal are prevented before they reach skin callbacks, instead of being erased after they happen.

Premature genuine circle misses use the same delayed presentation. Scoring still records their real miss and original timing immediately. Native combo-break audio is suppressed for this mod so it cannot reveal an early genuine miss. Both kinds of displayed circle miss trigger the same skinnable combo-break sound path.

Sliders and spinners are not phantom targets. Normal authoritative score, combo, accuracy, and results are preserved. This is an unofficial local-practice client.

## Regression coverage

The skin fixture creates paired native/unhit and successfully-hit phantom circles under each of the existing six skin configurations: Argon, Triangles, default legacy, Retro, metrics legacy test assets, and special legacy test assets with extrapolated animations. It compares component types, position, opacity, scale, size, colour, and deterministic rotation at controlled gameplay-clock times, including the inclusive hit-window boundary, the first timeout frame, and mid-fade. Native legacy MISS randomises its rotation on every playback. For that component, the comparison verifies transform times and normalised angular motion rather than demanding the same random angle.

Cases include early/on-time/late successful clicks, several OD and clock-rate values, hit animations/lighting on and off, Hidden and Classic callbacks, early genuine misses, drawable reuse, rewind between actual hit and visible miss, and live skin component reload before and after the miss. The paired tests do not replace the full-player tests, which also check real scoring, HUD concealment, mixed genuine/phantom misses, and target selection.

The configured rate cases exercise timestamp calculations using a manual clock; they are not a measurement of wall-clock audiovisual synchronisation. These tests compare drawable state rather than captured GPU pixels or audio waveforms. They do not establish human perceptual indistinguishability on every skin or hardware configuration. Actual misaims and recognisable slider/spinner mistakes can still reveal a broken FC.

## Preview builds

Branch pushes run the native Windows/Linux build, focused Phantom tests, and osu! mod-validity check. Passing runs produce portable client artifacts with actual `.trx` reports, `COMMIT.txt`, and `BUILD-CHANNEL.txt`. The release publishing job is restricted to `master` and does not run for this branch. Extract a preview into a new separate folder; do not overwrite the client/database currently being tested.
