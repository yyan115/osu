# Phantom Misses practice client

A personal osu!lazer fork for practising without authoritative live full-combo feedback. This is a complete client, not a DLL to install in the official client.

## Download

[Get the latest Windows and Linux release](https://github.com/yyan115/osu/releases/latest).

Choose `phantom-misses-win-x64.zip` for Windows or `phantom-misses-linux-x64.tar.gz` for Linux. Both include the .NET runtime; no SDK or compilation is required. GitHub's separate **Source code** archives are for development and are not the playable packages.

## Run on Windows

Requires 64-bit Windows with working graphics/audio drivers. Extract the entire Windows ZIP into a writable folder, close other osu! clients, then double-click **`run-phantom-misses.cmd`** inside `phantom-misses-win-x64`. Do not launch from inside the ZIP viewer. The binaries are unsigned and may trigger a Windows reputation warning.

## Run on Linux

Requires a current x86-64 Linux desktop with working graphics/audio drivers. Close other osu! clients. Extract the entire Linux archive into a writable folder, open a terminal inside `phantom-misses-linux-x64`, and run:

```bash
./run-phantom-misses.sh
```

## Storage and updates

The bundled `framework.ini` selects portable storage beside the executable. This starts with a separate database and settings instead of opening the normal lazer database. Keep this folder separate, do not replace it with the regular client folder, and do not redirect its storage to your regular installation. Always use the included launcher: it disables automatic updating through `OSU_EXTERNAL_UPDATE_PROVIDER` so the official updater cannot replace this fork.

Close other osu! clients before launching because the clients share an IPC name. Stay logged out for local practice. Import your `.osz` beatmaps and `.osk` skins by dragging them into this client. Existing maps can be exported from the normal client and imported here. The packages do not include third-party beatmaps.

For a newer release, extract into a new folder. Keep the old portable folder as your data backup; extracting a new package over it would replace the bundled configuration files. Export/import the maps and skins you need between the two clients.

## Use the mod

In osu!standard, open the mod selector and choose **Fun > Phantom Misses (PM)**. Start with the defaults:

| Setting | Default | Meaning |
| --- | ---: | --- |
| Average spacing | 180 | Later phantom gaps range from 90 to 270 eligible hit circles. |
| Warmup circles | 40 | The first target is chosen between circle indices 40 and 219. |
| Seed | Random | Leave unset for a generated seed; a fixed seed reproduces target selection. |

For a visible demonstration, use Autoplay + PM, set Average spacing to 30 and Warmup circles to 0, and play a circle-heavy map. Successful selected circles display MISS while final judgements remain real.

## Behaviour and scope

Only standalone hit circles are selected as phantom targets. In version 1.0.1, a successfully hit target retains its normal unhit appearance until the hit window expires, then uses native skinned miss presentation and combo-break audio. Its successful-hit sound and animation are suppressed. Premature genuine circle misses use the same delayed presentation. Genuine misses remain genuine in the real results. Score, combo, accuracy and health processing are not replaced with phantom results.

Live score/accuracy/combo/health HUD, separate playfield skin HUD content and break/replay overlays are concealed. Failure is prevented. These protections are mandatory; the old internal masking bindables are retained only for source compatibility and no longer control behaviour. The mod is unranked and unavailable in multiplayer.

Bubbles, Flashlight, No Scope, Bloom, Muted and Adaptive Speed are incompatible because they replace judgement presentation or react visibly to real gameplay state. Normal rate/difficulty settings remain available subject to osu!'s other compatibility rules.

This does not make real mistakes unknowable. Obvious misaims, slider breaks and spinner misses can still be recognised. Phantom targeting does not cover sliders or spinners. The unchanged warmup and minimum spacing also allow deductions about real misses. Frequency/selection changes are deferred. Automated skin coverage does not prove perceptual indistinguishability on every setup. The results screen always uses the real judgements.

## Verification and source

The **Phantom Misses Desktop** workflow builds and tests on native Windows and Linux runners separately, with warnings and code-style violations treated as errors. It runs the Phantom scoring, mixed-replay and skin fixtures, osu! mod-validity checks, and broader regressions in both threading modes before packaging self-contained Release clients. Packaging verifies that the intended Phantom fixtures were discovered and passed. Each package includes `COMMIT.txt`, `PHANTOM_VERSION`, and actual `.trx` reports under `verification/`.

The focused tests inspect active judgement containers, excluding preloaded pool placeholders. They check visible phantoms during a perfect run, full real combo/accuracy, sample masking, genuine misses, mandatory concealment, no-fail behaviour, deterministic target selection, warmup boundaries and spacing. Skin tests additionally exercise six configurations, animation/lighting settings, Hidden/Classic, drawable reuse, rewind and live skin reload. See [presentation and verification details](PHANTOM_FIDELITY.md).

The separate full CI retains all configured Windows/Linux test shards in both threading modes, Code Quality, Android/iOS builds and an aggregate test report. Automated tests do not establish compatibility with every graphics/audio driver or skin.

Pushes to `phantom-misses`, `fix/phantom-miss-fidelity` and `master` build both platforms. A successful `master` desktop run publishes `phantom-v<PHANTOM_VERSION>` only when that version has not already been released. Published releases are never overwritten. To publish an update, change `PHANTOM_VERSION` and `PHANTOM_RELEASE_NOTES.md` along with the code, verify the full CI, then merge into `master`. **Run workflow** on `master` can retry an unpublished version.

[Releases](https://github.com/yyan115/osu/releases) | [Builds](https://github.com/yyan115/osu/actions/workflows/phantom-misses.yml) | [Source on master](https://github.com/yyan115/osu/tree/master)

All development is in this personal fork. No upstream submission is intended.
