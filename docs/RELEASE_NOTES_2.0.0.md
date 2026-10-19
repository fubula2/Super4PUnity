# Super4PUnity 2.0.0

A big update: new engine, new video player, an in-app mod library, and builds for Windows, macOS, Android and (beta) Linux.

## Downloads

| Platform | File | Status |
|---|---|---|
| Windows 10/11 (64-bit) | `Super4PUnity_v2.0.0_windows_x64.zip` | Tested |
| macOS (Apple silicon) | `Super4PUnity_v2.0.0_macOS_arm64.zip` | Tested |
| Android 8+ (64-bit) | `Super4PUnity_v2.0.0_android_arm64.apk` | Tested |
| Linux (64-bit) | `Super4PUnity_v2.0.0_linux_x64.zip` | **Beta, barely tested** |

Unzip the whole folder (or install the APK), put your mod folders next to the app and run it. Details for each platform are in the README inside the download.

## What's new

**Engine and playback**
- Moved from Unity 2021 to Unity 6.
- New video player: clips switch smoothly with no stutter on scene changes, stay locked to the music, and keep playing clips whose last frames cannot be decoded.

**Get Mods (new)**
- Browse the community library inside the app: search, sort by Default / Newest / Oldest, double-click a series name to open its page. Patreon-only series open in the browser.
- Download zip and rar packs in the background, with a progress bar, a queue, notifications and a cancel button. Closing the window does not stop them.
- Folder shares let you pick several packs at once; choose which characters to import, and decide what to do when one already exists.
- **Music tab:** the music pack as folders you can expand and collapse, with search. Tracks download straight into `mods/music`.
- Works with MediaFire's current links.

**Everyday use**
- The `mods` folder always sits next to the app, so each download is one self-contained folder. "Mods Folder" opens a native folder picker; "Refresh" reloads.
- Search box for characters.
- Frame rate option (30 or 60) under Display Settings.
- Update notice when a newer version is released (it never installs anything by itself), and the version is shown in the app and in every file name.
- Artist link at the bottom of the menu.

**Android (new)**
- Touch support, a layout for wide phone screens, and a launcher icon.
- Pinch to switch between fit and fill, and double tap to hide the controls.
- Smoother video on weaker phones (30 fps default, smaller video buffer, pre-started clips).
- Mods folder: `Internal storage > Android > media > Super4PUnity.app > mods`. The Mods Folder button shows it and opens the Files app. A default music track is included.

**Linux (beta)**
- Unity's Linux player cannot play the clip format, so the first Play converts the selected videos (your originals are never changed). This needs `ffmpeg`, and the app offers to download it. This has had very little testing.

## Known issues

- macOS: the app is not notarized. Right-click it and choose Open the first time.
- Windows: SmartScreen may warn about an unknown publisher (More info, then Run anyway).
- The mod clips are 60 fps; budget phones can still stutter.
- Linux: beta; the Mods Folder button does not work yet.
- Android: sideload only (not on the Play Store). The app has a new Android ID (`Super4PUnity.app`), so it installs next to any older test build instead of replacing it.
