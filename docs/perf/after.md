# After: Unity 6000.6.6f1, VideoDeck player (macOS, development build)

Same scenario as `baseline.md` (`-autotest`, `DevPerfProbe`): Title -> Game with 3 mods + 1 track, 20 pose switches (a character switch every 4th), back to Title, twice.

## Results

| Run | Content | Frames over 40 ms | Over 100 ms | Worst (excl. startup) |
|---|---|---|---|---|
| Original player on Unity 6 | real library clips (~2 MB each) | 14 | 4 | 557.6 ms |
| Original player on Unity 6 | generated test clips (1280x720, 4 s, tiny) | 9 | 0 | 53 ms |
| VideoDeck | generated test clips (same) | 4 | 0 | 53 ms |

On the generated clips, every remaining frame over 40 ms falls on a Title <-> Game scene transition (scene load), none on a pose or character change. Music/video drift measured while playing stayed within about +/-30 ms with playback speed at 1.00.

## Caveat

The large stalls (100-557 ms) were only seen with the real, heavier clips, and the original player no longer exists in the tree, so the VideoDeck has **not** been re-measured on those clips. Re-run `-autotest` with the real library (point the Title screen's "Mods Folder" at it, or set `ModsPath` in `playerConfig.json`) to confirm.

## macOS build notes

- Default build is Apple silicon only. Universal (`-universal`) currently fails here because the editor's bundled `llvm-lipo` (`Contents/Resources/Burst/Client/bcl/hostmac/llvm-lipo`) lacks its execute permission; `chmod +x` on that file fixes it.
- Logs and config now live under `~/Library/Application Support/<company>/<product>/` instead of inside the app bundle, so the signed bundle stays sealed (`codesign --verify --deep --strict` passes after an ad-hoc signature).
- Default mods folder on macOS is `<Application Support>/<company>/<product>/mods`; the "Mods Folder" button overrides it.
- For distribution: sign with a Developer ID certificate, enable the hardened runtime, notarize with `notarytool`, and staple. The folder picker launches `/usr/bin/osascript`, which works unsandboxed; a Mac App Store (sandboxed) build would need a different picker.

## Get Mods verification

- Sheet: the live export parses to 150 entries (129 MediaFire files, 17 folders, 4 Patreon pages) in the real player, matching an independent count from the reference file.
- Folder links: a real folder listed 9 packs with names and sizes; the chooser lists them and Cancel works. A file link resolved to a `*.mediafire.com` direct URL. No pack was downloaded in these checks.
- UI in the real player: 18+ notice shown once and its acceptance saved; 150 rows load; search filters ("zelda" 1, no match 0, cleared 150); Close works.
- Offline: with a saved copy the list still opens and Reload shows a warning; with no saved copy it shows a clear error and "Press Reload to try again".
- Archives: zip and rar extraction pass edit-mode tests; rar covers RAR4, RAR5 and solid variants from the extraction library's own test archives (3 files each, none skipped). Traversal entries (`../`, absolute, drive letters) are skipped for both formats.
- Real pack (rar, 28 MB, library series "Boondocks"), into a scratch folder: downloaded, extracted and installed 2 mod folders (15 videos, 2 images); no files left in the work folder. Cancelling mid-download returned "Cancelled." and created nothing. Installing again over existing mods asked once; Skip kept them (0 installed, 2 skipped), Overwrite replaced them (a marker file in an old folder was gone).
- Still manual: a full zip pack install and clicking through the panel by hand (the checks above drive the same code and buttons from a test hook).

## Get Mods: background downloads (second round)

- Speed: the 28 MB test pack went from 62 s to about 1.4 s download (peak 18 MB/s; plain curl to the same server: ~29 MB/s). Cause was every awaited chunk resuming on Unity's main thread once per frame; the pipeline now runs on thread-pool threads with 1 MB buffers and polled progress.
- Background: closing the popup does not stop a download; a strip under the title bar shows title, percent, speed, time left, queue count and Cancel. Queue and duplicate rejection verified with two requests for the same pack.
- Import choice: with 2 characters in the pack a checklist appears (all ticked); None disables Import, All re-enables it; importing a subset installs only those; cancelling at the prompt or while it is open leaves nothing behind. A single character with named characters in the sheet imports without asking (unit-tested policy); a count-only or empty sheet cell always asks.
- Popup: Status column removed; sort cycles Sheet / Newest / Oldest with year headings (2026-2020 seen with live data) and is saved; Browser button per row; Patreon rows orange with the label "Patreon"; search accepts ":" and long text in both the Get Mods and Chars search boxes.
- Toasts after installs with Refresh and Show folder; overwrite/skip prompt verified (Skip: "installed 0 characters (2 skipped)").
- Not clicked by hand: Refresh and Show folder buttons on the toast, the Browser button (opens the default browser), the strip's Cancel button and click-to-open.
- Note: a mods folder that does not exist yet falls back to the default folder (existing behavior), which is where a test install landed; those test files were removed.
