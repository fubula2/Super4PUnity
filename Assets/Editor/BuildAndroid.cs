using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Android (ARM64) APK for sideloading. Signed with Unity's debug key, so it is for testing, not the Play Store.
//   Unity -batchmode -nographics -projectPath . -executeMethod BuildAndroid.Build
// Needs the Android Build Support module (with the Android SDK/NDK and OpenJDK).
public static class BuildAndroid
{
    public static void Build()
    {
        var version = PlayerSettings.bundleVersion;
        PlayerSettings.Android.bundleVersionCode = VersionCode(version);   //Android only installs an update with a higher code
        var output = "Builds/Android/Super4PUnity_v" + version + "_android_arm64.apk";
        Directory.CreateDirectory("Builds/Android");

        //The app's own media folder is named after this id (Android/media/<id>), so it carries the game's name. Android requires a dot in it.
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "Super4PUnity.app");

        SetIcons();

        //ARM64 needs IL2CPP; the project's saved settings were ARMv7 + Mono
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false;

        //Landscape only (either way up); the layout is built for a wide screen
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        var ok = report.summary.result == BuildResult.Succeeded;
        Debug.Log(ok ? "Android build: " + output + " (" + report.summary.totalSize + " bytes)" : "Android build failed: " + report.summary.result);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    //The launcher icon: the project's own S4PU tile instead of Unity's logo. Adaptive icons (Android 8+) use a pink background with
    //the artwork scaled into the always-visible middle; older and round icons use the tile as it is.
    private static void SetIcons()
    {
        var tile = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Single/ExeIcon.png");
        var background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/AndroidIcon/Adaptive_Background.png");
        var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/AndroidIcon/Adaptive_Foreground.png");
        if(tile == null) { Debug.LogWarning("ExeIcon.png not found; the Android icon is unchanged."); return; }

        //The Android icon kinds live in the Android module, so they are found by name and the script compiles without it
        var kinds = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Android.AndroidPlatformIconKind", false)).FirstOrDefault(t => t != null);
        if(kinds == null) { Debug.LogWarning("Android platform icon kinds not found; the Android icon is unchanged."); return; }

        foreach(var (name, layers) in new[] { ("Adaptive", new[] { background, foreground }), ("Round", new[] { tile }), ("Legacy", new[] { tile }) })
        {
            if(layers.Any(t => t == null)) { continue; }
            var kind = (PlatformIconKind)kinds.GetField(name)?.GetValue(null);
            if(kind == null) { continue; }
            try
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                foreach(var icon in icons) { icon.SetTextures(layers); }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
            }
            catch(Exception e) { Debug.LogWarning("Android " + name + " icon was not set: " + e.Message); }   //e.g. round icons are not part of this project's icon slots
        }
    }

    //2.0.0 -> 20000, 2.1.1 -> 20101: always rises with the version number
    private static int VersionCode(string version)
    {
        var parts = version.Split('.');
        int Part(int i) { return i < parts.Length && int.TryParse(parts[i], out var n) ? n : 0; }
        return Part(0) * 10000 + Part(1) * 100 + Part(2);
    }
}
