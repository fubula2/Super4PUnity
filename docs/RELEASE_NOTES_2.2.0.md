# Super4PUnity 2.2.0

Import mods and music straight from your device (no more copying folders by hand), plus fixes for phones that refused to read mod files.

## Downloads

| Platform | File | Status |
|---|---|---|
| Windows 10/11 (64-bit) | `Super4PUnity_v2.2.0_windows_x64.zip` | Tested |
| macOS (Apple silicon) | `Super4PUnity_v2.2.0_macOS_arm64.zip` | Tested |
| Android 8+ (64-bit) | `Super4PUnity_v2.2.0_android_arm64.apk` | Tested |
| Linux (64-bit) | `Super4PUnity_v2.2.0_linux_x64.zip` | **Beta** |

Unzip the whole folder (or install the APK), put your mod folders next to the app and run it. If you already use 2.1, copy your `mods` folder across (on macOS your settings are kept automatically; on Windows and Linux they sit next to the app).

## What's new

**Import from device**
- The **Mods Folder** button now opens a menu: **Import files**, **Open mods folder** and (on computers) **Choose mods folder**.
- **Import files** asks whether you are importing **characters** or **music**.
  - Characters: pick one or more `.zip` / `.rar` files, or **Char Folders**. The app extracts or copies them itself, shows what it found, and installs only the `CHAR_` folders you confirm. You are asked before extracting and again before installing.
  - Music: pick `.ogg` / `.mp3` files; they are copied into your music folder.
- **Android:** open or share a `.zip` / `.rar` to Super4PUnity from your Files app and the import window appears. Importing needs no storage permission.
- Linux: the picker needs `zenity` or `kdialog` installed.

**Android: files the app was not allowed to read**
- Some phones refuse to let the app read mod files that another app copied in, which left **Play** stuck or a mod blank. Mods imported through the app do not have this problem.
- Pressing **Play** now checks that the videos can be opened. If Android refuses, a window explains and offers **Allow access** (the "All files access" setting).

**Phones**
- The frame rate now defaults to **60** (you can still pick 30 in Display Settings).
- After pressing Play, a small **tip** explains pinch to zoom, with a "Don't remind me again" box.
- Pop-up windows now darken the whole screen on wide phones.

## Known issues

- macOS: the app is not notarized. Right-click it and choose Open the first time.
- Windows: SmartScreen may warn about an unknown publisher (More info, then Run anyway).
- The mod clips are 60 fps; budget phones can still stutter (pick 30 FPS in Display Settings).
- Multi-part rar files and password-protected archives cannot be imported.
- Linux: beta; ffmpeg is needed for video (the app offers to download it on 64-bit Intel/AMD machines).
- Android: sideload only (not on the Play Store).
