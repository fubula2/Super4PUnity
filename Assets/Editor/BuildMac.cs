using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildMac
{
    // Usage: -executeMethod BuildMac.Build [-dev] [-universal]
    // Release builds are packaged like the Windows version: a versioned folder with the app, an empty "mods" folder
    // and a short readme, signed (ad-hoc) and zipped. The Mac app uses the "mods" folder next to it by default.
    public static void Build()
    {
        var args = Environment.GetCommandLineArgs();
        var dev = args.Contains("-dev");
        var universal = args.Contains("-universal");

        //-universal builds Intel + Apple silicon (needs the editor's Burst llvm-lipo to be executable); default is Apple silicon
        UnityEditor.OSXStandalone.UserBuildSettings.architecture = universal
            ? UnityEditor.Build.OSArchitecture.x64ARM64
            : UnityEditor.Build.OSArchitecture.ARM64;
        //Bundle id so the app can be signed
        var target = UnityEditor.Build.NamedBuildTarget.Standalone;
        if(string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(target)))
        {
            PlayerSettings.SetApplicationIdentifier(target, "com.nephtri.super4punity");
        }

        var version = PlayerSettings.bundleVersion;
        //The release is assembled from a staging build, so Unity's "BackUpThisFolder" debug folder is never packaged
        var buildPath = dev ? "Builds/macOS/Super4PUnity.app" : "Builds/macOS/staging/Super4PUnity.app";
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = buildPath,
            target = BuildTarget.StandaloneOSX,
            options = dev ? BuildOptions.Development | BuildOptions.ConnectWithProfiler : BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        var ok = report.summary.result == BuildResult.Succeeded;

        if(ok && !dev) { ok = Package(version, universal ? "universal" : "arm64"); }
        EditorApplication.Exit(ok ? 0 : 1);
    }

    private static bool Package(string version, string architecture)
    {
        var folderName = "Super4PUnity_v" + version;
        var release = "Builds/macOS/" + folderName;
        var zip = "Builds/macOS/" + folderName + "_macOS_" + architecture + ".zip";

        if(Directory.Exists(release)) { Directory.Delete(release, true); }
        if(File.Exists(zip)) { File.Delete(zip); }
        //The package's mods folder: whatever is in Packaging/mods (the default music track in music/), plus an empty voices folder
        CopyFolder("Packaging/mods", release + "/mods");
        Directory.CreateDirectory(release + "/mods/music");
        Directory.CreateDirectory(release + "/mods/voices");

        File.WriteAllText(release + "/README.txt",
            "Super4PUnity " + version + "\n\n" +
            "1. Put your mod folders (CHAR_...) in the \"mods\" folder next to Super4PUnity.app. Music goes in mods/music\n" +
            "   (one track is included) and voices in mods/voices.\n" +
            "   The app always looks for it next to itself and creates it if it is missing.\n" +
            "   Or use the \"Mods Folder\" button in the app to pick another folder.\n" +
            "2. Open Super4PUnity.app. If macOS says it cannot verify the app, right-click the app, choose Open, then Open.\n" +
            "3. The \"Get Mods\" button can download character packs and music into the mods folder for you (Characters and Music tabs).\n\n" +
            "To update, unzip the new version and copy your \"mods\" folder across (settings are kept automatically).\n");

        return Run("/usr/bin/ditto", "Builds/macOS/staging/Super4PUnity.app", release + "/Super4PUnity.app")
            && BuildScrub.ReplaceHomePath(release + "/Super4PUnity.app") >= 0   //before signing, which covers the changed files
            && Run("/usr/bin/codesign", "--force", "--deep", "-s", "-", release + "/Super4PUnity.app")
            //No resource forks / extended attributes: they would show up as "._name" files when unzipped elsewhere
            && Run("/usr/bin/ditto", "-c", "-k", "--keepParent", "--norsrc", "--noextattr", "--noqtn", "--noacl", release, zip)
            && Cleanup("Builds/macOS/staging")
            && Log("Release files: " + release + " and " + zip);
    }

    private static void CopyFolder(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        if(!Directory.Exists(source)) { return; }

        foreach(var file in Directory.GetFiles(source))
        {
            if(Path.GetFileName(file).StartsWith(".")) { continue; }
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }
        foreach(var dir in Directory.GetDirectories(source)) { CopyFolder(dir, Path.Combine(destination, Path.GetFileName(dir))); }
    }

    private static bool Cleanup(string folder)
    {
        if(Directory.Exists(folder)) { Directory.Delete(folder, true); }
        return true;
    }

    private static bool Log(string message)
    {
        UnityEngine.Debug.Log(message);
        return true;
    }

    private static bool Run(string file, params string[] arguments)
    {
        var info = new ProcessStartInfo(file) { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true };
        foreach(var a in arguments) { info.ArgumentList.Add(a); }
        using(var p = Process.Start(info))
        {
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if(p.ExitCode != 0) { UnityEngine.Debug.LogError(file + " failed: " + err); }
            return p.ExitCode == 0;
        }
    }
}
