#if UNITY_STANDALONE || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

//Native multi-select file/folder dialogs: osascript on macOS, PowerShell on Windows, zenity or kdialog on Linux
public class DesktopImportPicker : MonoBehaviour, IImportPicker
{
    private volatile IReadOnlyList<string> pendingResult;
    private Action<IReadOnlyList<string>> pendingCallback;
    private bool isOpen;

    public string UnavailableReason
    {
        get
        {
            #if (UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX)
                return LinuxTool() == null ? "To pick files here, install zenity or kdialog, or copy the mods into the mods folder yourself." : null;
            #else
                return null;
            #endif
        }
    }

    public bool OneFolderAtATime
    {
        get
        {
            #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return true;
            #elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
                return LinuxTool() == "kdialog";
            #else
                return false;
            #endif
        }
    }

    public IReadOnlyList<string> TakeTempDirs() { return new List<string>(); }

    public void PickFiles(bool audio, Action<IReadOnlyList<string>> onPicked) { Run(false, audio, onPicked); }
    public void PickFolders(Action<IReadOnlyList<string>> onPicked) { Run(true, false, onPicked); }

    private void Run(bool folders, bool audio, Action<IReadOnlyList<string>> onPicked)
    {
        if(isOpen) { return; }
        isOpen = true;
        pendingCallback = onPicked;
        Task.Run(() =>
        {
            try { pendingResult = RunDialog(folders, audio); }
            catch(Exception e) { UnityEngine.Debug.LogWarning("Import dialog failed: " + e.Message); pendingResult = new List<string>(); }
        });
    }

    private void Update()
    {
        if(pendingResult == null) { return; }
        var result = pendingResult;
        pendingResult = null;
        isOpen = false;
        if(result.Count > 0) { pendingCallback?.Invoke(result); }
    }

    private static IReadOnlyList<string> RunDialog(bool folders, bool audio)
    {
        var info = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            info.FileName = "powershell";
            info.ArgumentList.Add("-NoProfile");
            info.ArgumentList.Add("-STA");
            info.ArgumentList.Add("-Command");
            info.ArgumentList.Add("[Console]::OutputEncoding=[Text.Encoding]::UTF8; Add-Type -AssemblyName System.Windows.Forms; " + (folders
                ? "$d=New-Object System.Windows.Forms.FolderBrowserDialog; $d.Description='Choose a folder with CHAR_ mods'; if($d.ShowDialog() -eq 'OK'){[Console]::Out.Write($d.SelectedPath)}"
                : "$d=New-Object System.Windows.Forms.OpenFileDialog; $d.Multiselect=$true; " + (audio ? "$d.Filter='Music (*.ogg;*.mp3)|*.ogg;*.mp3|All files (*.*)|*.*'; $d.Title='Choose music files';" : "$d.Filter='Mod archives (*.zip;*.rar)|*.zip;*.rar|All files (*.*)|*.*'; $d.Title='Choose mod archives';") + " if($d.ShowDialog() -eq 'OK'){[Console]::Out.Write(($d.FileNames -join \"`n\"))}"));
        #elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            var tool = LinuxTool();
            if(tool == null) { return new List<string>(); }
            info.FileName = tool;
            if(tool == "zenity")
            {
                info.ArgumentList.Add("--file-selection"); info.ArgumentList.Add("--multiple"); info.ArgumentList.Add("--separator=\n");
                info.ArgumentList.Add("--title=Import mods");
                if(folders) { info.ArgumentList.Add("--directory"); }
                else { info.ArgumentList.Add(audio ? "--file-filter=Music | *.ogg *.mp3 *.OGG *.MP3" : "--file-filter=Mod archives | *.zip *.rar *.ZIP *.RAR"); }
            }
            else
            {
                info.ArgumentList.Add(folders ? "--getexistingdirectory" : "--getopenfilename");
                info.ArgumentList.Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                if(!folders) { info.ArgumentList.Add(audio ? "*.ogg *.mp3|Music" : "*.zip *.rar|Mod archives"); info.ArgumentList.Add("--multiple"); info.ArgumentList.Add("--separate-output"); }
            }
        #else
            info.FileName = "/usr/bin/osascript";
            //One path per output line so names with commas or spaces survive
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("on run argv");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("if item 1 of argv is \"folders\" then");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("set picked to choose folder with prompt \"Choose folders with CHAR_ mods\" with multiple selections allowed");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("else");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("set picked to choose file with prompt (item 2 of argv) with multiple selections allowed");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("end if");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("set out to \"\"");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("repeat with f in picked");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("set out to out & (POSIX path of f) & linefeed");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("end repeat");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("return out");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("end run");
            info.ArgumentList.Add(folders ? "folders" : "files");
            info.ArgumentList.Add(audio ? "Choose music files (.ogg, .mp3)" : "Choose mod archives (.zip, .rar)");
        #endif

        using(var process = Process.Start(info))
        {
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            //Non-zero exit means cancelled (or failed); treat both as "no change"
            return process.ExitCode == 0 ? PickerOutput.ParseLines(output) : new List<string>();
        }
    }

    #if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    private static string tool;
    private static bool toolChecked;

    private static string LinuxTool()
    {
        if(toolChecked) { return tool; }
        toolChecked = true;
        foreach(var name in new[] { "zenity", "kdialog" })
        {
            foreach(var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(':'))
            {
                if(dir.Length > 0 && File.Exists(Path.Combine(dir, name))) { tool = name; return tool; }
            }
        }
        return null;
    }
    #endif
}
#endif
