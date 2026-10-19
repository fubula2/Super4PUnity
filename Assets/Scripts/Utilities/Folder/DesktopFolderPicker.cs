#if UNITY_STANDALONE || UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

//Native folder dialog on macOS and Windows, using the OS's own scripting hosts
public class DesktopFolderPicker : MonoBehaviour, IFolderPicker
{
    private volatile string pendingResult;
    private Action<string> pendingCallback;
    private bool isOpen;

    public void Pick(string title, string startPath, Action<string> onPicked)
    {
        if(isOpen) { return; }
        isOpen = true;
        pendingCallback = onPicked;

        Task.Run(() =>
        {
            try { pendingResult = RunDialog(title, startPath) ?? string.Empty; }
            catch(Exception e) { UnityEngine.Debug.LogWarning("Folder dialog failed: " + e.Message); pendingResult = string.Empty; }
        });
    }

    private void Update()
    {
        if(pendingResult == null) { return; }

        var result = pendingResult;
        pendingResult = null;
        isOpen = false;

        if(result.Length > 0) { pendingCallback?.Invoke(result); }
    }

    private static string RunDialog(string title, string startPath)
    {
        var info = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        #if UNITY_STANDALONE_WIN
            info.FileName = "powershell";
            info.ArgumentList.Add("-NoProfile");
            info.ArgumentList.Add("-STA");
            info.ArgumentList.Add("-Command");
            info.ArgumentList.Add("[Console]::OutputEncoding=[Text.Encoding]::UTF8; Add-Type -AssemblyName System.Windows.Forms; " +
                "$d=New-Object System.Windows.Forms.FolderBrowserDialog; $d.Description=$env:S4P_TITLE; " +
                "if($env:S4P_START){$d.SelectedPath=$env:S4P_START}; " +
                "if($d.ShowDialog() -eq 'OK'){[Console]::Out.Write($d.SelectedPath)}");
            info.EnvironmentVariables["S4P_TITLE"] = title;
            info.EnvironmentVariables["S4P_START"] = Directory.Exists(startPath) ? startPath : string.Empty;
        #else
            info.FileName = "/usr/bin/osascript";
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("on run argv");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("if (count of argv) > 1 then");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("return POSIX path of (choose folder with prompt (item 1 of argv) default location (POSIX file (item 2 of argv)))");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("end if");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("return POSIX path of (choose folder with prompt (item 1 of argv))");
            info.ArgumentList.Add("-e"); info.ArgumentList.Add("end run");
            info.ArgumentList.Add(title);
            if(Directory.Exists(startPath)) { info.ArgumentList.Add(startPath); }
        #endif

        using(var process = Process.Start(info))
        {
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            //Non-zero exit means cancelled (or failed); treat both as "no change"
            return process.ExitCode == 0 ? output : null;
        }
    }
}
#endif
