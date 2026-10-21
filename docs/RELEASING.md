# Releasing a new version

1. Bump the version: Unity > Edit > Project Settings > Player > Version (for example `1.2.2`).
   The version shows in the app (top-left of the Title screen) and in the file names below.
2. Build (macOS): `Unity -batchmode -nographics -projectPath . -executeMethod BuildMac.Build -quit`
   This creates, in `Builds/macOS/`:
   - `Super4PUnity_v<version>/` containing `Super4PUnity.app` (ad-hoc signed), a `mods` folder and `README.txt`.
     The `mods` folder holds `music/` (the files from `Packaging/mods/music`, currently the default track) and an empty `voices/`.
   - `Super4PUnity_v<version>_macOS_arm64.zip` with that folder inside (no `._` junk files)
   Windows and Linux (x64) packages: `Unity -batchmode -nographics -projectPath . -executeMethod BuildDesktop.Windows`
   (or `BuildDesktop.Linux`) after installing the matching Unity build module. They create
   `Builds/Windows/Super4PUnity_v<version>_windows_x64.zip` and `Builds/Linux/Super4PUnity_v<version>_linux_x64.zip`
   with the same folder layout (player files, `mods/` with `music` and `voices`, `README.txt`). The architecture is forced to x64.
3. Publish: create a GitHub Release tagged `v<version>` (for example `v1.2.2`) and attach the zip(s).
   The first line of the release notes is shown in the in-app notice.

Package layout (matches the Windows version): everything lives in one versioned folder, and the app always assumes
its `mods` folder is next to it (created, with `music` and `voices`, if missing; a per-user folder is used only if
that location is not writable). On macOS this holds even when macOS runs a freshly downloaded app from a temporary
copy (the app looks up its real location). Settings and logs are kept per user in `~/Library/Application Support/`, so
updating means unzipping the new folder and copying the `mods` folder across.

How players are told: on each launch (at most once a day) the app asks GitHub for the latest release. If its tag is a
newer version than the running app, a notice appears with Download (opens the release page) and Skip version.
Clicking the version label at any time checks right away. Drafts and pre-releases are ignored. The app never downloads
or installs anything by itself.

## Get Mods sources

- The Characters list and the Music tab both come from the public library sheet (tab `ModsDownloads`). The music folder link is the hyperlink on the cell labelled "Music Mods"; the app reads it each time the list loads.
- Music providers live behind `IMusicSource` (`Assets/Scripts/Core`). Google Drive is `DriveLinks`; when the sheet's link moves to another host, add a provider for it and register it in `MusicService.Sources`. An unknown host shows "Music source not supported yet" in the tab.
- Downloaded tracks are saved flat into `mods/music`.

## Linux video playback

- Unity's Linux player cannot decode the H.264 clips, so on Linux the app converts the clips of the selected characters (first Play only) into VP8/Vorbis WebM copies in `mods/.cache/video`, using ffmpeg. Originals are never modified. See `LinuxVideoPrep` / `LinuxPlayFlow`.
- ffmpeg is taken from the app's own `mods/.tools/ffmpeg/` copy, else from `PATH`. If neither works, the player is asked before the app downloads the pinned build (`FfmpegInstaller.Url`, SHA-256 checked). The pinned file is the permanent "old-releases" static x86-64 build 6.0.1 (GPL build, downloaded from its original host at the player's request, not bundled in our zip). To change it: update `Url`, `Sha256`, `ApproxBytes` and `InnerPath`, and confirm `ffmpeg -encoders` lists libvpx and libvorbis.
- Not yet verified on a real Linux machine: that Unity's Linux player plays the WebM copies. If it does not, switch `FfmpegArgs.Convert` and `VideoCache.Extension` to Theora in `.ogv` (`-c:v libtheora`).

## Android

- Build: `Unity -batchmode -nographics -projectPath . -executeMethod BuildAndroid.Build` (needs the Android module with SDK/NDK/OpenJDK). Output: `Builds/Android/Super4PUnity_v<ver>_android_arm64.apk`. ARM64 only, IL2CPP, signed with Unity's debug key (sideloading only; the Play Store needs a release keystore and an app bundle).
- The mods folder is `Android/media/Super4PUnity.app/mods` on internal storage (the folder is named after the Android application id, set in `BuildAndroid`; Android needs a dot in it) (with `music` and `voices`), created on first launch. The Files app shows `Android/media` on every Android version and the app needs no storage permission there (unlike `Android/data`, which Android 11+ hides). The "Mods Folder" button shows the path and offers to open it in Files.
- Phone layout (`MobileLayout`): no desktop resolution/fullscreen options, the scrolling background is scaled to cover wide screens, and the menu content is centred (`CanvasContentCentering`). Double tap on the picture hides/shows the controls; pinching out/in fills the screen with the video or shows it whole with bars (remembered). Layout can be previewed on a Mac with the dev build and `S4P_FORCE_MOBILE_LAYOUT=1`.
- Not yet run on a device.

## Phone performance

- Clips in the mod packs are 60 fps H.264 (checked on one pack: 241 frames in 4.02 s). Budget phones struggle to decode two at once, which is what the deck does to switch clips without a pause.
- On phones (`MobileLayout.Active`): 30 fps cap and no screen sleep (`MobilePerformance`), a smaller video buffer chosen from the phone's RAM (`VideoBufferPolicy`), and no playback-speed nudges (only the seek at each loop boundary).
- `PerfOverlay`: F3, or touching the screen with three fingers, shows fps, slowest frame, memory and the playing clip's size and frame rate.
- Biggest remaining lever: lower-resolution, 30 fps copies of the clips for phones (made on a computer with ffmpeg).

- Phone video (`VideoDeck`): the picture fills the screen (cropped, `EnvelopeParent`); the queued clip is started silently and paused on its first frame so the switch needs no decoder start-up; seeks are skipped for drift under 0.35 s because each seek stalls a phone's player. Behaviour on a real phone is not yet confirmed.

- Android default music: `AndroidDefaultMusic` copies `Packaging/mods/music/*.ogg` into StreamingAssets only while an Android build runs; `DefaultMusicCopier` copies them into the music folder the first time the app starts. Changing the application id installs as a separate app (settings and mods of the old id are not carried over).
- Android icon: `BuildAndroid.SetIcons` uses the project's `Assets/Sprites/Single/ExeIcon.png` (adaptive icon = pink background + the artwork scaled into the always-visible middle, from `Assets/Sprites/AndroidIcon`). Only adaptive icons are set; the app needs Android 8+ anyway (min SDK 26), where launchers use them.

## Publishing a release

1. Bump `bundleVersion` (Project Settings > Player). Android's version code is derived from it by `BuildAndroid`.
2. Write `docs/RELEASE_NOTES_<version>.md` (what's new, platform status, known issues).
3. Run the unit tests, then build every package (`BuildMac.Build`, `BuildDesktop.Windows`, `BuildDesktop.Linux`, `BuildAndroid.Build`).
4. Commit, tag `v<version>` and push. The tag must match the version: the app's update notice compares the latest release's tag with its own version.
5. Create the GitHub release for that tag, use the release notes as its description, and attach the four packages.

## Import from device
The Mods Folder button opens a menu: Import files, Open mods folder, and (desktop) Choose mods folder. Import files asks for characters or music. Characters: pick `.zip`/`.rar` files or folders; they are extracted or copied into a temp folder, the player confirms what was picked and then which `CHAR_*` mods were found, and only `CHAR_*` folders are installed. Music: pick `.ogg`/`.mp3` files, copied flat into `mods/music`.
- Windows/macOS use PowerShell/osascript dialogs; Linux needs `zenity` or `kdialog` installed (a message says so otherwise).
- Android uses the system picker through `Assets/Plugins/Android/S4PPicker.java`, which copies the picked content into the app cache (`cache/import/`), deleted when the import ends. No storage permission is needed.
- Android: when Play finds a video the system refuses to open, a window offers "Allow access" (All files access, declared in `Assets/Plugins/Android/S4PAccess.androidlib`). The androidlib's `build.gradle` hardcodes compileSdk 36 and build tools 36.0.0; update them when the Unity version changes.
- Android: `.zip`/`.rar` files opened with, or shared to, the app go straight to the import confirmation (intent filters in `S4PAccess.androidlib/AndroidManifest.xml`; the library's `build.gradle` points `sourceSets.main.manifest` at it, otherwise the manifest is silently ignored).
