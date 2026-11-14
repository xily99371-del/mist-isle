# Force-sync the local checkout

The GitHub branch `polish/full-game-character-art` is the source of truth for the current playtest candidate.

Run these commands in PowerShell from the local `mist-isle` repository root:

```powershell
git fetch origin --prune
git switch -C polish/full-game-character-art origin/polish/full-game-character-art
git reset --hard origin/polish/full-game-character-art
git clean -fd
```

This resets tracked files to the remote branch and removes untracked, non-ignored files. It does not remove ignored build output. If a local save matters, copy `mist_isle_save.dat` somewhere safe before syncing; the save is runtime data, not a source asset.

To confirm the exact revision:

```powershell
git rev-parse --short HEAD
git status --short --branch
```

After syncing, use the repository's `RUN_WINDOWS.bat` or the CMake build instructions in `README.md`. Do not copy old `assets`, `maps`, or generated headers over the newly synced checkout.
