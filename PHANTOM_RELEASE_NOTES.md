## Downloads

These are complete, self-contained clients. No SDK, source build, or separate .NET installation is required.

| Platform | Download | Start after extracting the whole archive |
| --- | --- | --- |
| Windows x64 | `phantom-misses-win-x64.zip` | Double-click `run-phantom-misses.cmd` |
| Linux x64 | `phantom-misses-linux-x64.tar.gz` | Run `./run-phantom-misses.sh` |

Choose the platform package under **Assets**, not GitHub's automatically generated **Source code** archives. The `.sha256` files contain checksums for the packages.

Close other osu! clients first. Extract into a separate writable folder and use the included launcher. Each package uses a separate portable database and settings, and the launcher disables the official updater. Do not install over your normal osu! folder or redirect storage to its database.

Stay logged out for local practice. Import `.osz` beatmaps and `.osk` skins, then select **osu!standard > Fun > Phantom Misses (PM)**. No third-party maps or skins are bundled.

## Included

- Phantom misses on selected successfully hit standalone circles, with miss visuals and combo-break audio.
- Concealed live combo, score, accuracy and health, with failure prevention.
- Real judgements preserved for the results screen.
- Configurable average spacing, warmup and seed.
- Native Windows and Linux builds, each gated by the Phantom regression suite and mod-validity tests. Test reports and source commit are included in each package.

This is an unofficial personal practice fork, unranked and unavailable in multiplayer. Phantom targeting does not cover sliders or spinners. Obvious misaims and unusual skins can still reveal real mistakes. Windows binaries are unsigned and may trigger a Windows reputation warning. Automated tests are not a guarantee of compatibility with every graphics/audio driver or skin.
