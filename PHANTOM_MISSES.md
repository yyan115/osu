# Phantom Misses practice client

A personal osu!lazer fork for practising without authoritative live full-combo feedback. This is a complete client, not a DLL to install in the official client.

## Run the Linux package

Requires a current x86-64 Linux desktop with working graphics/audio drivers. The package includes the .NET runtime; no SDK or compilation is required.

Close any other osu! client first because the clients share an IPC name. Extract the entire `phantom-misses-linux-x64.tar.gz` archive into a writable folder, open a terminal there, and run:

```bash
./run-phantom-misses.sh
```

The bundled `framework.ini` selects portable storage beside the executable. This starts with a separate database and settings instead of opening the normal lazer database. Keep this folder separate, do not replace it with the regular client folder, and do not redirect its storage to your regular installation. The launcher disables automatic updating through `OSU_EXTERNAL_UPDATE_PROVIDER` so the official updater cannot replace this fork.

Stay logged out for local practice. Import your `.osz` beatmaps and `.osk` skins by dragging them into this client. Existing maps can be exported from the normal client and imported here. The package does not include third-party beatmaps.

In osu!standard, open the mod selector and choose **Fun > Phantom Misses (PM)**. Start with the defaults:

| Setting | Default | Meaning |
| --- | ---: | --- |
| Average spacing | 180 | Later phantom gaps range from 90 to 270 eligible hit circles. |
| Warmup circles | 40 | The first target is chosen between circle indices 40 and 219. |
| Seed | Random | Leave unset for a generated seed; a fixed seed reproduces target selection. |

For a visible demonstration, use Autoplay + PM, set Average spacing to 30 and Warmup circles to 0, and play a circle-heavy map. Successful selected circles display MISS while final judgements remain real.

## Behaviour and scope

Only standalone hit circles are selected as phantom targets. Successfully hitting a selected target shows a skinned miss, suppresses its hitsound, applies a miss-like fade and supplies combo-break audio. Genuine misses remain genuine in the real results. Score, combo, accuracy and health processing are not replaced with phantom results.

Live score/accuracy/combo/health HUD, separate playfield skin HUD content and break/replay overlays are concealed. Failure is prevented. These protections are mandatory; the old internal masking bindables are retained only for source compatibility and no longer control behaviour. The mod is unranked and unavailable in multiplayer.

Bubbles, Flashlight, No Scope, Bloom, Muted and Adaptive Speed are incompatible because they replace judgement presentation or react visibly to real gameplay state. Normal rate/difficulty settings remain available subject to osu!'s other compatibility rules.

This does not make real mistakes unknowable. Obvious misaims, slider breaks and spinner misses can still be recognised. Phantom targeting does not cover sliders or spinners, and unusual skins may retain distinguishable hit/miss details. The results screen always uses the real judgements.

## Verification and source

The `Phantom Misses Linux` workflow builds the desktop solution with warnings treated as errors, runs the focused Phantom test fixture and osu! mod-validity checks, then publishes and packages a self-contained Release client. The package includes `COMMIT.txt` and the actual `.trx` reports under `verification/`.

The focused tests inspect active judgement containers, excluding preloaded pool placeholders. They check visible phantoms during a perfect run, full real combo/accuracy, sample masking, genuine misses, mandatory concealment, no-fail behaviour, deterministic target selection, warmup boundaries and spacing.

Builds and workflow artifacts: https://github.com/yyan115/osu/actions/workflows/phantom-misses.yml

Source branch: https://github.com/yyan115/osu/tree/phantom-misses

Development PR is inside this personal fork only: https://github.com/yyan115/osu/pull/1

No upstream submission is intended.
