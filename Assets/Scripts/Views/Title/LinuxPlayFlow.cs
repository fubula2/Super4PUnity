using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

//Linux only: before the game scene opens, makes sure every selected clip has a converted copy (see LinuxVideoPrep).
//Asks before downloading ffmpeg when the computer has no usable one.
public static class LinuxPlayFlow
{
    private static bool busy;

    public static bool Busy => busy;

    public static async void Run(Dialog dialog, IReadOnlyList<string> selectedMods, Action play)
    {
        if(busy) { return; }
        busy = true;
        try { await RunAsync(dialog, selectedMods, play); }
        catch(Exception e)
        {
            Debug.LogWarning("Video preparation failed: " + e);
            await Notice(dialog, "Videos could not be prepared", e.Message);
        }
        finally { busy = false; }
    }

    private static async Task RunAsync(Dialog dialog, IReadOnlyList<string> selectedMods, Action play)
    {
        var modsDir = GameDataManager.GM.GetModsPath();
        var clips = selectedMods.SelectMany(m => GameDataManager.GM.GetVideoPaths(m)).ToList();
        var todo = LinuxVideoPrep.Pending(clips, modsDir);
        if(todo.Count == 0) { play(); return; }

        var cts = new CancellationTokenSource();
        var toolsDir = LinuxVideoPrep.ToolsDir(modsDir);
        var ffmpeg = await FfmpegLocator.FindAsync(toolsDir, CancellationToken.None);

        if(ffmpeg == null)
        {
            var agreed = await Ask(dialog, "ffmpeg needed",
                "Linux needs ffmpeg to convert these videos once for playback. Your original videos are not changed.\n\nDownload ffmpeg now (about " + TransferFormat.Size(FfmpegInstaller.ApproxBytes) + ")?",
                "Download", "Cancel");
            if(!agreed) { return; }

            dialog.Show("Downloading ffmpeg", "Starting...", "Cancel", () => cts.Cancel());
            float fraction = 0;
            var install = Task.Run(() => FfmpegInstaller.InstallAsync(toolsDir, f => fraction = f, cts.Token));
            while(!install.IsCompleted) { dialog.BodyText.text = "Downloading ffmpeg... " + Mathf.RoundToInt(fraction * 100) + "%"; await Task.Delay(250); }
            if(cts.IsCancellationRequested) { dialog.Hide(); return; }
            try { ffmpeg = await install; }
            catch(InvalidOperationException e) { dialog.Hide(); await Notice(dialog, "ffmpeg was not installed", e.Message); return; }
            dialog.Hide();
        }

        var done = 0;
        var total = todo.Count;
        dialog.Show("Preparing videos", "Converting videos for playback (first time only)...", "Cancel", () => cts.Cancel());
        var convert = Task.Run(() => LinuxVideoPrep.ConvertAsync(ffmpeg, todo, modsDir, (d, t) => { done = d; total = t; }, cts.Token));
        while(!convert.IsCompleted) { dialog.BodyText.text = "Preparing videos (" + done + "/" + total + ")\nThis only happens the first time."; await Task.Delay(250); }
        var result = await convert;
        dialog.Hide();

        if(result.Cancelled) { return; }
        if(result.Failed.Count == 0) { play(); return; }

        var list = string.Join("\n", result.Failed.Take(6)) + (result.Failed.Count > 6 ? "\n..." : string.Empty);
        if(result.Converted == 0 && result.Failed.Count == todo.Count)
        {
            await Notice(dialog, "Videos could not be prepared", list);
            return;
        }
        var playAnyway = await Ask(dialog, "Some videos could not be prepared", list + "\n\nPlay with the rest?", "Play", "Back");
        if(playAnyway) { play(); }
    }

    private static Task<bool> Ask(Dialog dialog, string title, string body, string yes, string no)
    {
        var tcs = new TaskCompletionSource<bool>();
        dialog.Show(title, body, yes, () => tcs.TrySetResult(true), no, () => tcs.TrySetResult(false));
        return tcs.Task;
    }

    private static Task Notice(Dialog dialog, string title, string body)
    {
        var tcs = new TaskCompletionSource<bool>();
        dialog.Show(title, body, "OK", () => tcs.TrySetResult(true));
        return tcs.Task;
    }
}
