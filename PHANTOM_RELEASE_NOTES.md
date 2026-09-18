## Downloads

These are complete, self-contained clients. No SDK, source build, or separate .NET installation is required.

| Platform | Download | Start after extracting the whole archive |
| --- | --- | --- |
| Windows x64 | `phantom-misses-win-x64.zip` | Double-click `run-phantom-misses.cmd` |
| Linux x64 | `phantom-misses-linux-x64.tar.gz` | Run `./run-phantom-misses.sh` |

Choose the platform package under **Assets**, not GitHub's automatically generated **Source code** archives. The `.sha256` files contain package checksums.

Close other osu! clients first. Extract into a new, separate writable folder and use the included launcher. Keep your old portable folder as a backup. Do not extract over your regular osu! installation or redirect this client to its database. Each package uses portable storage and the launcher disables the official updater.

Stay logged out for local practice. Import `.osz` beatmaps and `.osk` skins, then select **osu!standard > Fun > Phantom Misses (PM)**. No third-party maps or skins are bundled.

## Changes in 1.0.1

This release brings the tested presentation and stability fixes from PR #2 into the default branch.

- Phantom circle misses now appear at the normal hit-window timeout rather than at the successful click. Premature genuine circle misses use the same delayed presentation; authoritative judgement timing and scoring remain unchanged.
- Successful-hit skin animations are prevented before skin callbacks run. Native miss presentation is retained through drawable reuse, rewind and live skin reload. Coverage includes six skin configurations, including animated legacy assets.
- Displayed circle misses share the skinnable combo-break audio path so the native scoring-driven sound cannot reveal an early genuine miss.
- Fixed HUD dependency registration before asynchronous skin loading, cached leaderboard results interfering with a fresh fetch, and WebSocket shutdown/cancellation races.
- Hardened import-test host fault handling and bounded startup/background-work waits without disabling failing assertions. Expanded Windows/Linux regression gates.

**Miss selection is unchanged:** standalone hit circles only, average spacing 180 and warmup 40 by default, seeded target selection independent of whether the run is still an FC. The planned frequency/selection redesign is not part of this release. Mandatory live HUD/health concealment and failure prevention remain; final results use real judgements.

## Verification

The gameplay and stability implementation at `ae38811a21ab973fb6968b45b3247c99e4083639` passed the [full CI suite](https://github.com/yyan115/osu/actions/runs/35349899715) and [native Windows/Linux desktop gates](https://github.com/yyan115/osu/actions/runs/35349892657). The [native socket stress run](https://github.com/yyan115/osu/actions/runs/35349986411) passed 1,000 shutdown cycles on each platform with zero direct or unobserved errors.

Release preparation updates version information and documentation without changing that implementation. The release pipeline rebuilds both platforms and reruns Phantom, mod-validity and broader regressions in both threading modes before packaging. Each package includes its actual `.trx` reports under `verification/`, `COMMIT.txt`, `BUILD-CHANNEL.txt` and `PHANTOM_VERSION`. The exact release build is linked below.

## Scope and limitations

This is an unofficial personal practice fork, unranked and unavailable in multiplayer. Sliders and spinners are not phantom targets. Obvious misaims, recognisable slider/spinner mistakes, and the unchanged warmup/spacing rules can reveal that an FC has broken. Visual tests compare drawable state, not every GPU-rendered frame or audio waveform; universal perceptual indistinguishability is not claimed. Windows binaries are unsigned and may trigger a reputation warning. Compatibility with every skin, driver and hardware setup has not been established.
