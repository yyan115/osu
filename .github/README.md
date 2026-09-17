# osu!lazer with Phantom Misses

Inspired from: https://www.reddit.com/r/osugame/comments/1wigvnt/crazy_new_idea_for_an_osu_mod/

An unofficial fork of [osu!lazer](https://github.com/ppy/osu). Selected successful circles can look and sound like misses while the real judgements are preserved for the results screen. Live combo, score, accuracy and health are hidden during play for now (WIP).

## Download and play

**[Download the latest release for Windows or Linux](https://github.com/yyan115/osu/releases/latest)**

| Platform | Asset | Launch after extracting the entire archive |
| --- | --- | --- |
| Windows x64 | `phantom-misses-win-x64.zip` | `run-phantom-misses.cmd` |
| Linux x64 | `phantom-misses-linux-x64.tar.gz` | `./run-phantom-misses.sh` |

No source build or separate .NET installation is required. Use a separate writable folder, close other osu! clients, and start through the included launcher. The package keeps a separate portable database and disables official auto-updating through the launcher. Stay logged out, import your beatmaps/skins, then enable **osu!standard > Fun > Phantom Misses (PM)**.

**[Usage, settings, limitations and development workflow](../PHANTOM_MISSES.md)**

Phantom targeting currently covers standalone hit circles only. The mod is unranked and unavailable in multiplayer. Obvious mistakes and some skin details can still reveal genuine misses. Windows binaries are unsigned.

## Source and verification

The playable modification is on this repository's default branch, **`master`**. The [Phantom Misses Desktop workflow](https://github.com/yyan115/osu/actions/workflows/phantom-misses.yml) builds and tests Windows and Linux separately. Releases are published only after both succeed, with test reports, checksums and source commit information.

osu!lazer and its original assets/code are by ppy and contributors. This fork is not an official osu! release and has not been submitted upstream. See the [original project README](../README.md) and [MIT licence](../LICENCE).
