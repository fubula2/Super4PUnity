using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class PrepResult
{
    public int Converted;
    public bool Cancelled;
    public readonly List<string> Failed = new List<string>();   //"clip name: reason"
}

//Unity's Linux player cannot decode the H.264 clips, so Linux plays converted copies kept in <mods>/.cache/video.
//The original clips are only ever read.
public static class LinuxVideoPrep
{
    public static bool Needed
    {
        get
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(Environment.GetEnvironmentVariable("S4P_FORCE_LINUX_VIDEO") == "1") { return true; }
#endif
            return Application.platform == RuntimePlatform.LinuxPlayer;
        }
    }

    public static string CacheDir(string modsDir) { return Path.Combine(modsDir, ".cache", "video"); }
    public static string ToolsDir(string modsDir) { return Path.Combine(modsDir, ".tools"); }

    private static string CachedPath(string cacheDir, string source)
    {
        var info = new FileInfo(source);
        return VideoCache.CachePath(cacheDir, VideoCache.KeyFor(source, info.Length, info.LastWriteTimeUtc));
    }

    private static bool IsValid(string path)
    {
        try { var f = new FileInfo(path); return f.Exists && f.Length > 0; } catch(IOException) { return false; }
    }

    //The file the video player should open for a clip: its converted copy when there is one
    public static string Resolve(string original)
    {
        if(!Needed || !File.Exists(original)) { return original; }
        var cached = CachedPath(CacheDir(GameDataManager.GM.GetModsPath()), original);
        return IsValid(cached) ? cached : original;
    }

    //Clips among these that have no converted copy yet
    public static IReadOnlyList<VideoCache.Clip> Pending(IEnumerable<string> clipPaths, string modsDir)
    {
        var clips = clipPaths.Where(File.Exists).Select(p => { var i = new FileInfo(p); return new VideoCache.Clip { Source = p, Size = i.Length, ModifiedUtc = i.LastWriteTimeUtc }; });
        return VideoCache.Plan(clips, CacheDir(modsDir), IsValid);
    }

    //Converts the clips a few at a time. progress(done, total) may be called from any thread.
    public static async Task<PrepResult> ConvertAsync(string ffmpeg, IReadOnlyList<VideoCache.Clip> todo, string modsDir, Action<int, int> progress, CancellationToken ct)
    {
        var result = new PrepResult();
        var cacheDir = CacheDir(modsDir);
        try { Directory.CreateDirectory(cacheDir); }
        catch(Exception e) when(e is IOException || e is UnauthorizedAccessException)
        {
            result.Failed.Add("Could not create " + cacheDir + ": " + e.Message);
            return result;
        }

        var done = 0;
        var gate = new SemaphoreSlim(Mathf.Clamp(Environment.ProcessorCount / 2, 1, 4));
        progress?.Invoke(0, todo.Count);

        await Task.WhenAll(todo.Select(async clip =>
        {
            await gate.WaitAsync().ConfigureAwait(false);
            var final = CachedPath(cacheDir, clip.Source);
            var temp = final + ".tmp";
            try
            {
                if(ct.IsCancellationRequested) { return; }
                var run = await ProcessRunner.RunAsync(ffmpeg, FfmpegArgs.Convert(clip.Source, temp), ct).ConfigureAwait(false);
                if(run.ExitCode == 0 && new FileInfo(temp).Length > 0)
                {
                    if(File.Exists(final)) { File.Delete(final); }
                    File.Move(temp, final);
                    lock(result) { result.Converted++; }
                }
                else
                {
                    lock(result) { result.Failed.Add(Path.GetFileName(clip.Source) + ": " + LastLine(run.Output)); }
                }
            }
            catch(OperationCanceledException) { result.Cancelled = true; }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is System.ComponentModel.Win32Exception)
            {
                lock(result) { result.Failed.Add(Path.GetFileName(clip.Source) + ": " + e.Message); }
            }
            finally
            {
                try { if(File.Exists(temp)) { File.Delete(temp); } } catch(IOException) { }
                progress?.Invoke(Interlocked.Increment(ref done), todo.Count);
                gate.Release();
            }
        })).ConfigureAwait(false);

        if(ct.IsCancellationRequested) { result.Cancelled = true; }
        return result;
    }

    private static string LastLine(string output)
    {
        var line = (output ?? string.Empty).Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Length > 0);
        return line ?? "ffmpeg could not convert this clip";
    }
}
