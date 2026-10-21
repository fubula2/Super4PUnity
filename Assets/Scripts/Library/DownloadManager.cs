using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public enum DownloadKind { Pack, Music, Import }

public sealed class DownloadRequest
{
    public DownloadKind Kind;   //Pack: PageUrl is a MediaFire page; Music: FileUrl is a direct file link saved flat as FileName
    public string FileUrl;
    public string FileName;
    public string Key;          //unique per series/pack; also used to show row state in the popup
    public string Title;
    public string PageUrl;
    public long ExpectedBytes;
    public bool NamesKnown;     //the sheet names the characters (not just a count)
    public string ModsDir;
    public string WorkDir;
    public IReadOnlyList<string> Sources;      //Import: local archives/folders picked on the device
    public IReadOnlyList<string> CleanupDirs;  //Import: temporary copies to delete when the job ends
    public bool ImportMusic;                   //Import: the sources are .ogg/.mp3 files copied flat into ModsDir
    internal ImportPreview Preview;            //Import: what was shown to the player, for the final message
}

public enum PromptKind { Select, Conflict }

//A question the worker is waiting on; the Title screen shows it and completes it
public sealed class PendingPrompt
{
    public PromptKind Kind;
    public bool IsMusic;
    public bool IsImport;
    public string Note;   //extra line shown under the title (import: skipped items)
    public string Title;
    public IReadOnlyList<string> Names;
    internal readonly TaskCompletionSource<IReadOnlyList<string>> Selection = new TaskCompletionSource<IReadOnlyList<string>>();
    internal readonly TaskCompletionSource<ConflictChoice> Conflict = new TaskCompletionSource<ConflictChoice>();

    //Folder names to import; null or empty cancels the import
    public void CompleteSelect(IReadOnlyList<string> chosen) { Selection.TrySetResult(chosen); }
    public void CompleteConflict(ConflictChoice choice) { Conflict.TrySetResult(choice); }
}

public sealed class DownloadNotification
{
    public string Text;
    public bool IsError;
    public bool CanRefresh;
    public string ModsDir;

    //Update notices reuse the toast: Download opens the release page, Skip hides this version
    public bool IsUpdate;
    public string Url;
    public string Version;
}

public struct DownloadSnapshot
{
    public bool Active;
    public string Key;
    public string Title;
    public string Stage;
    public float Fraction;
    public double BytesPerSecond;
    public double SecondsLeft;
    public bool WaitingForPrompt;
    public IReadOnlyList<string> QueuedKeys;
}

//Runs queued pack installs one at a time on thread-pool threads. Static so downloads survive the popup
//closing and scene loads. UI polls Snapshot / PendingPrompt / TryTakeNotification each frame.
public static class DownloadManager
{
    private static readonly object Gate = new object();
    private static readonly DownloadQueue<DownloadRequest> Queue = new DownloadQueue<DownloadRequest>();
    private static readonly ConcurrentQueue<DownloadNotification> Notifications = new ConcurrentQueue<DownloadNotification>();

    private static DownloadRequest current;
    private static CancellationTokenSource currentCts;
    private static bool running;
    private static PackProgress progress;
    private static PendingPrompt prompt;

    private static long lastBytes;
    private static DateTime lastSample;
    private static double speed;

    public static PendingPrompt PendingPrompt { get { lock(Gate) { return prompt; } } }

    //False when this key is already running or queued
    public static bool Enqueue(DownloadRequest request)
    {
        lock(Gate)
        {
            if(current != null && current.Key == request.Key) { return false; }
            if(!Queue.Enqueue(request.Key, request)) { return false; }
            if(!running) { running = true; Task.Run(WorkerLoop); }
            return true;
        }
    }

    //Queues a device import; false when one is already running or queued
    public static bool EnqueueImport(IReadOnlyList<string> sources, string modsDir, string workDir, IReadOnlyList<string> cleanupDirs, bool music = false)
    {
        return Enqueue(new DownloadRequest
        {
            Kind = DownloadKind.Import, Key = "import:" + Guid.NewGuid().ToString("N"), Title = music ? "Music import" : "Device import", ImportMusic = music,
            Sources = sources, ModsDir = modsDir, WorkDir = workDir, CleanupDirs = cleanupDirs
        });
    }

    public static bool RemoveQueued(string key) { lock(Gate) { return Queue.Remove(key); } }

    public static void CancelCurrent() { lock(Gate) { currentCts?.Cancel(); } }

    public static bool IsActiveOrQueued(string key)
    {
        lock(Gate) { return (current != null && current.Key == key) || Queue.Contains(key); }
    }

    public static DownloadSnapshot Snapshot
    {
        get
        {
            lock(Gate)
            {
                if(current == null) { return new DownloadSnapshot { QueuedKeys = Queue.Keys.ToList() }; }
                var total = progress.BytesTotal;
                var left = speed > 1 && total > 0 ? (total - progress.BytesDone) / speed : double.NaN;
                return new DownloadSnapshot
                {
                    Active = true, Key = current.Key, Title = current.Title, Stage = progress.Stage, Fraction = progress.Fraction,
                    BytesPerSecond = speed, SecondsLeft = left, WaitingForPrompt = prompt != null, QueuedKeys = Queue.Keys.ToList()
                };
            }
        }
    }

    public static bool TryTakeNotification(out DownloadNotification notification) { return Notifications.TryDequeue(out notification); }

    //Removes leftovers from an interrupted run; does nothing while a job is active
    public static void CleanupStaleWork(string workDir)
    {
        lock(Gate)
        {
            if(running) { return; }
            foreach(var sub in new[] { "downloads", "extract" })
            {
                try { var dir = Path.Combine(workDir, sub); if(Directory.Exists(dir)) { Directory.Delete(dir, true); } }
                catch(IOException) { }
                catch(UnauthorizedAccessException) { }
            }
        }
    }

    private static async Task WorkerLoop()
    {
        while(true)
        {
            DownloadRequest request;
            CancellationToken ct;
            lock(Gate)
            {
                if(!Queue.TryDequeue(out request)) { running = false; current = null; currentCts = null; prompt = null; return; }
                current = request;
                currentCts = new CancellationTokenSource();
                ct = currentCts.Token;
                progress = new PackProgress("Downloading", 0f);
                lastBytes = 0; lastSample = DateTime.UtcNow; speed = 0;
            }

            InstallResult result;
            try
            {
                if(request.Kind == DownloadKind.Import)
                {
                    result = await ImportJob.RunAsync(request, preview => SelectImport(request, preview, ct), names => Conflict(request, names, ct), OnProgress, ct).ConfigureAwait(false);
                }
                else if(request.Kind == DownloadKind.Music)
                {
                    result = await MusicDownloadJob.RunAsync(request, names => Conflict(request, names, ct), OnProgress, ct).ConfigureAwait(false);
                }
                else
                {
                    result = await new PackInstallJob().RunAsync(request.PageUrl, request.ExpectedBytes, request.ModsDir, request.WorkDir,
                        names => Select(request, names, ct), names => Conflict(request, names, ct), OnProgress, ct).ConfigureAwait(false);
                }
            }
            catch(Exception e) { result = new InstallResult { Error = "Unexpected error: " + e.Message }; }

            lock(Gate) { prompt = null; }
            Notifications.Enqueue(Describe(request, result));
        }
    }

    private static IReadOnlyList<string> Select(DownloadRequest request, IReadOnlyList<string> names, CancellationToken ct)
    {
        if(!ImportPolicy.NeedsPrompt(names.Count, request.NamesKnown)) { return names; }

        var p = new PendingPrompt { Kind = PromptKind.Select, Title = request.Title, Names = names };
        lock(Gate) { prompt = p; }
        try { return p.Selection.Task.Wait(Timeout.Infinite, ct) ? p.Selection.Task.Result : null; }
        finally { lock(Gate) { if(prompt == p) { prompt = null; } } }
    }

    //A device import always asks, even for a single character
    private static IReadOnlyList<string> SelectImport(DownloadRequest request, ImportPreview preview, CancellationToken ct)
    {
        request.Preview = preview;
        var p = new PendingPrompt { Kind = PromptKind.Select, IsImport = true, Title = request.Title, Names = preview.Names, Note = ImportNote(preview) };
        lock(Gate) { prompt = p; }
        try { return p.Selection.Task.Wait(Timeout.Infinite, ct) ? p.Selection.Task.Result : null; }
        finally { lock(Gate) { if(prompt == p) { prompt = null; } } }
    }

    private static string ImportNote(ImportPreview preview)
    {
        var lines = new List<string>();
        if(preview.NotMods.Count > 0) { lines.Add("Skipped, not mods: " + string.Join(", ", preview.NotMods.Take(3)) + (preview.NotMods.Count > 3 ? " and " + (preview.NotMods.Count - 3) + " more" : "")); }
        if(preview.Unreadable.Count > 0) { lines.Add("Could not read: " + string.Join(", ", preview.Unreadable.Take(3)) + (preview.Unreadable.Count > 3 ? " and " + (preview.Unreadable.Count - 3) + " more" : "")); }
        if(preview.Duplicates.Count > 0) { lines.Add(preview.Duplicates.Count + " duplicate " + (preview.Duplicates.Count == 1 ? "name" : "names") + " ignored"); }
        return string.Join("\n", lines);
    }

    private static ConflictChoice Conflict(DownloadRequest request, IReadOnlyList<string> names, CancellationToken ct)
    {
        var p = new PendingPrompt { Kind = PromptKind.Conflict, Title = request.Title, Names = names, IsMusic = request.Kind == DownloadKind.Music || request.ImportMusic };
        lock(Gate) { prompt = p; }
        try { p.Conflict.Task.Wait(ct); return p.Conflict.Task.Result; }
        finally { lock(Gate) { if(prompt == p) { prompt = null; } } }
    }

    private static void OnProgress(PackProgress p)
    {
        lock(Gate)
        {
            progress = p;
            var now = DateTime.UtcNow;
            var dt = (now - lastSample).TotalSeconds;
            if(p.Stage == "Downloading" && dt >= 0.25)
            {
                var instant = (p.BytesDone - lastBytes) / dt;
                speed = speed <= 0 ? instant : speed * 0.7 + instant * 0.3;
                lastBytes = p.BytesDone;
                lastSample = now;
            }
        }
    }

    private static DownloadNotification Describe(DownloadRequest request, InstallResult result)
    {
        if(result.Ok && request.Kind == DownloadKind.Music)
        {
            if(result.Installed.Count == 0) { return new DownloadNotification { Text = request.Title + ": skipped (already in your music folder)" }; }
            return new DownloadNotification { Text = request.Title + ": added to music", CanRefresh = true, ModsDir = request.ModsDir };
        }
        if(result.Ok && request.ImportMusic)
        {
            var text = request.Title + ": added " + result.Installed.Count + (result.Installed.Count == 1 ? " track" : " tracks") + " to music";
            if(result.Skipped.Count > 0) { text += " (" + result.Skipped.Count + " skipped)"; }
            return new DownloadNotification { Text = text, CanRefresh = true, ModsDir = request.ModsDir };
        }
        if(result.Ok && request.Kind == DownloadKind.Import)
        {
            var text = request.Title + ": installed " + result.Installed.Count + (result.Installed.Count == 1 ? " character" : " characters");
            var extra = new List<string>();
            if(result.Skipped.Count > 0) { extra.Add(result.Skipped.Count + " skipped"); }
            var preview = request.Preview;
            if(preview != null && preview.NotMods.Count > 0) { extra.Add(preview.NotMods.Count + " not mods"); }
            if(preview != null && preview.Unreadable.Count > 0) { extra.Add(preview.Unreadable.Count + " unreadable"); }
            if(extra.Count > 0) { text += " (" + string.Join(", ", extra) + ")"; }
            return new DownloadNotification { Text = text, CanRefresh = true, ModsDir = request.ModsDir };
        }
        if(result.Ok)
        {
            var text = request.Title + ": installed " + result.Installed.Count + (result.Installed.Count == 1 ? " character" : " characters");
            if(result.Skipped.Count > 0) { text += " (" + result.Skipped.Count + " skipped)"; }
            return new DownloadNotification { Text = text, CanRefresh = true, ModsDir = request.ModsDir };
        }
        if(result.Error == "Cancelled.") { return new DownloadNotification { Text = request.Title + ": cancelled" }; }
        return new DownloadNotification { Text = request.Title + ": " + result.Error, IsError = true };
    }
}
