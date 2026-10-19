using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Windows (x64) and Linux (x64) release packages, laid out like the original Windows build: one versioned folder with
// the player files, an empty-ish "mods" folder (music + voices) and a short readme, zipped.
//   Unity -batchmode -nographics -projectPath . -executeMethod BuildDesktop.Windows
//   Unity -batchmode -nographics -projectPath . -executeMethod BuildDesktop.Linux
// Needs the matching Unity build module (Windows Build Support / Linux Build Support).
public static class BuildDesktop
{
    public static void Windows()
    {
        Package(BuildTarget.StandaloneWindows64, "Windows", "Super4PUnity.exe", "windows_x64",
            "1. Unzip the whole folder somewhere you can write to (not Program Files).\n" +
            "2. Put your mod folders (CHAR_...) in the \"mods\" folder next to Super4PUnity.exe. Music goes in mods\\music\n" +
            "   (one track is included) and voices in mods\\voices.\n" +
            "   The app always looks for it next to itself and creates it if it is missing.\n" +
            "   Or use the \"Mods Folder\" button in the app to pick another folder.\n" +
            "3. Run Super4PUnity.exe. If Windows SmartScreen warns about an unknown publisher, choose \"More info\", then \"Run anyway\".\n" +
            "4. The \"Get Mods\" button can download character packs and music into the mods folder for you (Characters and Music tabs).\n\n" +
            "To update, unzip the new version and copy your \"mods\" folder across.\n");
    }

    public static void Linux()
    {
        Package(BuildTarget.StandaloneLinux64, "Linux", "Super4PUnity.x86_64", "linux_x64",
            "THE LINUX VERSION IS A BETA. It has had very little testing, so expect rough edges.\n\n" +
            "1. Unzip the whole folder somewhere you can write to.\n" +
            "2. Put your mod folders (CHAR_...) in the \"mods\" folder next to Super4PUnity.x86_64. Music goes in mods/music\n" +
            "   (one track is included) and voices in mods/voices.\n" +
            "   The app always looks for it next to itself and creates it if it is missing.\n" +
            "3. Make the file executable if needed (chmod +x Super4PUnity.x86_64) and run it.\n" +
            "   The \"Mods Folder\" button does not work on Linux yet; copy mods into the mods folder by hand.\n" +
            "4. Videos on Linux: the first time you press Play, the app converts the selected videos for playback (your originals are not changed;\n" +
            "   copies go to mods/.cache). This needs ffmpeg. If it is not installed, the app offers to download it into mods/.tools.\n" +
            "   You can also install it yourself (for example: sudo pacman -S ffmpeg, or sudo apt install ffmpeg).\n\n" +
            "To update, unzip the new version and copy your \"mods\" folder across.\n");
    }

    private static void Package(BuildTarget target, string platform, string executable, string zipSuffix, string readmeSteps)
    {
        var version = PlayerSettings.bundleVersion;
        var staging = "Builds/" + platform + "/staging";
        var folderName = "Super4PUnity_v" + version;
        var release = "Builds/" + platform + "/" + folderName;
        var zip = "Builds/" + platform + "/" + folderName + "_" + zipSuffix + ".zip";

        //Always x64: without this Unity follows the editor machine's CPU (an Apple-silicon Mac would produce ARM64)
        SetArchitecture(platform == "Windows" ? "UnityEditor.WindowsStandalone.UserBuildSettings" : "UnityEditor.LinuxStandalone.UserBuildSettings");

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = staging + "/" + executable,
            target = target,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        var ok = report.summary.result == BuildResult.Succeeded;

        if(ok)
        {
            if(Directory.Exists(release)) { Directory.Delete(release, true); }
            if(File.Exists(zip)) { File.Delete(zip); }

            //ditto keeps file permissions (the Linux executable must stay executable)
            ok = Run("/usr/bin/ditto", "--norsrc", "--noextattr", "--noqtn", staging, release);
            if(ok)
            {
                //Debug-symbol folders Unity writes next to the player must not ship
                foreach(var dir in Directory.GetDirectories(release).Where(d => d.EndsWith("DoNotShip") || d.EndsWith("ButDontShipItWithYourGame"))) { Directory.Delete(dir, true); }

                BuildScrub.ReplaceHomePath(release);
                CopyFolder("Packaging/mods", release + "/mods");
                Directory.CreateDirectory(release + "/mods/music");
                Directory.CreateDirectory(release + "/mods/voices");
                File.WriteAllText(release + "/README.txt", "Super4PUnity " + version + " (" + platform + ")\n\n" + readmeSteps);

                //zip (not ditto) so the archive is plain and keeps permissions; -X drops Mac extra attributes
                ok = RunIn("Builds/" + platform, "/usr/bin/zip", "-r", "-X", Path.GetFileName(zip), folderName);
                if(ok && Directory.Exists(staging)) { Directory.Delete(staging, true); }
            }
            UnityEngine.Debug.Log(ok ? "Release files: " + release + " and " + zip : "Packaging failed for " + platform);
        }
        EditorApplication.Exit(ok ? 0 : 1);
    }

    //Set through reflection so the editor scripts still compile when a platform module is not installed
    private static void SetArchitecture(string typeName)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName, false)).FirstOrDefault(t => t != null);
        var property = type?.GetProperty("architecture", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if(property == null) { UnityEngine.Debug.LogWarning("Could not set the build architecture through " + typeName); return; }
        property.SetValue(null, UnityEditor.Build.OSArchitecture.x64);
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

    private static bool Run(string file, params string[] arguments) { return RunIn(null, file, arguments); }

    private static bool RunIn(string workingDirectory, string file, params string[] arguments)
    {
        var info = new ProcessStartInfo(file) { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true };
        if(workingDirectory != null) { info.WorkingDirectory = workingDirectory; }
        foreach(var a in arguments) { info.ArgumentList.Add(a); }
        using(var p = Process.Start(info))
        {
            p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if(p.ExitCode != 0) { UnityEngine.Debug.LogError(file + " failed: " + err); }
            return p.ExitCode == 0;
        }
    }
}
