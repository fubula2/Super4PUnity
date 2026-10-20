# Super4PUnity

This is a recreation of 4PUnity by Jexom, rebuilt on a current engine with a new video player, an in-app mod library and releases for several platforms.

## Download

Get the latest build from the [Releases](../../releases) page. Each download is one self-contained folder (or one APK): unzip it, put your mods next to the app, run it.

| Platform | File | Status |
|---|---|---|
| Windows 10/11 (64-bit) | `Super4PUnity_v2.1.1_windows_x64.zip` | Tested |
| macOS (Apple silicon) | `Super4PUnity_v2.1.1_macOS_arm64.zip` | Tested |
| Android 8+ (64-bit) | `Super4PUnity_v2.1.1_android_arm64.apk` | Tested |
| Linux (64-bit) | `Super4PUnity_v2.1.1_linux_x64.zip` | **Beta** |

## Features

- **Smooth playback.** Clips switch without a pause and stay locked to the music's 4-second bar.
- **Get Mods.** Browse the community library from inside the app, with a Characters tab and a Music tab.
  - Search, sort (Default / Newest / Oldest), double-click a series name to open its page.
  - Downloads zip or rar packs in the background with a progress bar, a queue and notifications. Folder shares let you pick several packs at once, and you choose which characters to import.
  - The Music tab shows the music pack as folders you can expand and collapse; tracks download straight into `mods/music`.
- **Mods next to the app.** The `mods` folder (with `music` and `voices`) always sits beside the app. A "Mods Folder" button lets you pick another one, and "Refresh" reloads it.
- **Window and full screen.** Resize or maximize the window freely. F11 switches full screen on and off in the menu and the player; in the player a double click does the same, and a size picker sits at the top centre (it hides with the other controls when you press U).
- **Frame rate option** (30 or 60) under Display Settings.
- **Update notice.** The app tells you when a newer version is released; it never updates itself silently.
- **Phone support.** Touch layout, pinch to switch between fit and fill, double tap to hide the controls.

## Install notes

- **Windows:** unzip somewhere you can write to (not Program Files). If SmartScreen warns about an unknown publisher, choose More info, then Run anyway.
- **macOS:** the app is not notarized. The first time, right-click it and choose Open.
- **Android:** allow installing from unknown sources for your browser or file manager, then open the APK. Mods go in `Internal storage > Android > media > Super4PUnity.app > mods`. The Mods Folder button shows the path and opens it in the Files app.
- **Linux (beta):** make `Super4PUnity.x86_64` executable and run it. Videos are converted once for playback, which needs `ffmpeg`; the app offers to download it if it is missing. The Mods Folder button does not work yet.

## Building

Unity 6000.6.6f1. Open the project, or use the scripts in `Assets/Editor` (`BuildMac`, `BuildDesktop`, `BuildAndroid`). See [docs/RELEASING.md](docs/RELEASING.md) for the full release steps. Put a default track in `Packaging/mods/music` and it is included in the packages.

## Credits

- Originally forked from Jexom's 4PUnity project: https://github.com/jexom/4PUnity
- [SharpCompress](https://github.com/adamhathcock/sharpcompress) (MIT) reads rar archives.
- On Linux the optional ffmpeg download comes from its own host and is not part of this repository.
- Mod packs, music and artwork belong to their creators.
