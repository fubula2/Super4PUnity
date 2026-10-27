# Super4PUnity 2.3.0

Series logos in Get Mods, an update pop-up when a new version is out, and a few small fixes to pop-ups.

## Downloads

| Platform | File | Status |
|---|---|---|
| Windows 10/11 (64-bit) | `Super4PUnity_v2.3.0_windows_x64.zip` | Tested |
| macOS (Apple silicon) | `Super4PUnity_v2.3.0_macOS_arm64.zip` | Tested |
| Android 8+ (64-bit) | `Super4PUnity_v2.3.0_android_arm64.apk` | Tested |
| Linux (64-bit) | `Super4PUnity_v2.3.0_linux_x64.zip` | **Beta** |

Unzip the whole folder (or install the APK), put your mod folders next to the app and run it. If you already use 2.2, copy your `mods` folder across (on macOS your settings are kept automatically; on Windows and Linux they sit next to the app).

## What's new

**Get Mods**
- Each series now shows its **logo** to the left of its name, taken from the library sheet. Series without a logo show the sheet's letters in the Lobster font instead.
- On the "mod downloaded" pop-up, the **Refresh** and **Open folder** buttons are wider.

**Updates**
- The app now checks for a new version **every time it starts**. If you are out of date, a pop-up offers **Download** or **Later**, with a **Don't show again** box (it skips that version; the next release asks again). The version label still turns red when you are out of date.

## Known issues

- macOS: the app is not notarized. Right-click it and choose Open the first time.
- Windows: SmartScreen may warn about an unknown publisher (More info, then Run anyway).
- The mod clips are 60 fps; budget phones can still stutter (pick 30 FPS in Display Settings).
- Multi-part rar files and password-protected archives cannot be imported.
- Linux: beta; the import picker needs `zenity` or `kdialog`, and ffmpeg is needed for video (the app offers to download it on 64-bit Intel/AMD machines).
- Android: sideload only (not on the Play Store).
