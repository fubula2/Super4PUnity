# Super4PUnity 2.1.0

An update to 2.0 with full screen controls, an easier-to-see top bar and faster video conversion on Linux.

## Downloads

| Platform | File | Status |
|---|---|---|
| Windows 10/11 (64-bit) | `Super4PUnity_v2.1.0_windows_x64.zip` | Tested |
| macOS (Apple silicon) | `Super4PUnity_v2.1.0_macOS_arm64.zip` | Tested |
| Android 8+ (64-bit) | `Super4PUnity_v2.1.0_android_arm64.apk` | Tested |
| Linux (64-bit) | `Super4PUnity_v2.1.0_linux_x64.zip` | **Beta** |

Unzip the whole folder (or install the APK), put your mod folders next to the app and run it. If you already use 2.0, copy your `mods` folder across. On macOS your settings are kept automatically; on Windows and Linux they sit next to the app.

## What's new

**Computers (Windows, macOS, Linux)**
- **Full screen without leaving the player.** Press **F11** in the menu or the player, or double-click the picture in the player. (The old **W** key still works.)
- **Window size picker at the top centre of the player.** It hides and shows with the other controls when you press **U**.
- **The picture always stays whole and centred**, so a window of any shape never crops it.

**Easier to find**
- The **Get Mods**, **Mods Folder** and **Refresh** buttons are now green with white text, and Get Mods and Refresh are wider, so they stand out from the yellow bar.

**Linux (beta)**
- Converting clips for playback is much faster: the encoder runs in its fast mode and uses as many cores as your machine has. Your clips keep their original size and frame rate, and your originals are never changed.

## Known issues

- macOS: the app is not notarized. Right-click it and choose Open the first time.
- Windows: SmartScreen may warn about an unknown publisher (More info, then Run anyway).
- The mod clips are 60 fps; budget phones can still stutter.
- Linux: beta; the Mods Folder button does not work yet, and ffmpeg is needed (the app offers to download it on 64-bit Intel/AMD machines).
- Android: sideload only (not on the Play Store).
