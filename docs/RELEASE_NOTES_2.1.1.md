# Super4PUnity 2.1.1

A small update that fixes mods whose videos did not show up on some phones, and makes it clearer when a new version is out.

## Downloads

| Platform | File | Status |
|---|---|---|
| Windows 10/11 (64-bit) | `Super4PUnity_v2.1.1_windows_x64.zip` | Tested |
| macOS (Apple silicon) | `Super4PUnity_v2.1.1_macOS_arm64.zip` | Tested |
| Android 8+ (64-bit) | `Super4PUnity_v2.1.1_android_arm64.apk` | Tested |
| Linux (64-bit) | `Super4PUnity_v2.1.1_linux_x64.zip` | **Beta** |

Unzip the whole folder (or install the APK), put your mod folders next to the app and run it. If you already use 2.1, copy your `mods` folder across (on macOS your settings are kept automatically; on Windows and Linux they sit next to the app).

## What's new

**Fixes**
- **Mods with a blank screen on Android (and Linux).** A mod was listed but showed no videos or icon when its files sat one folder deeper (for example `CHAR_X/CHAR_X/clip.mp4`, which happens when you unzip by hand) or used capital letters in their extensions (`.MP4`, `.PNG`). The app now finds them either way, for videos, icons and music.
- **No more blank screen for an empty mod.** If a mod really has no video files, the app now says so and lists what the folder holds. Such mods are marked "(no videos)" in the list.

**Small improvements**
- The **version label turns red** when a newer release is out, and it sits a little further from the corner of the bar.
- The on-screen readout (**F3**, or touch the screen with three fingers) now shows the latest video error, which helps when a clip will not play.
- **Android:** the app's log files are now in `Android/media/Super4PUnity.app/logs`, where the Files app can show them.

## Known issues

- macOS: the app is not notarized. Right-click it and choose Open the first time.
- Windows: SmartScreen may warn about an unknown publisher (More info, then Run anyway).
- The mod clips are 60 fps; budget phones can still stutter.
- Linux: beta; the Mods Folder button does not work yet, and ffmpeg is needed (the app offers to download it on 64-bit Intel/AMD machines).
- Android: sideload only (not on the Play Store).
