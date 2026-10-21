using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

//Imports archives/folders picked on the device. Runs on a thread-pool thread (see DownloadManager).
public static class ImportJob
{
    public static async Task<InstallResult> RunAsync(DownloadRequest request, Func<ImportPreview, IReadOnlyList<string>> onSelect,
        Func<IReadOnlyList<string>, ConflictChoice> onConflict, Action<PackProgress> progress, CancellationToken ct)
    {
        try
        {
            if(request.ImportMusic)
            {
                progress?.Invoke(new PackProgress("Extracting", 0f));
                var copy = new InlineProgress<float>(f => progress?.Invoke(new PackProgress("Extracting", f)));
                return await Task.Run(() => MusicImporter.Import(request.Sources, request.ModsDir, onConflict, copy, ct), ct).ConfigureAwait(false);
            }
            progress?.Invoke(new PackProgress("Extracting", 0f));
            var inner = new InlineProgress<float>(f => progress?.Invoke(new PackProgress("Extracting", f)));
            var tempRoot = Path.Combine(request.WorkDir, "extract");
            return await Task.Run(() => ImportInstaller.Install(request.Sources, request.ModsDir, tempRoot, onSelect, onConflict, inner, ct), ct).ConfigureAwait(false);
        }
        catch(OperationCanceledException) { return new InstallResult { Error = "Cancelled." }; }
        finally
        {
            //Copies made by the Android picker are ours to remove; the picked originals are never touched
            if(request.CleanupDirs != null)
            {
                foreach(var dir in request.CleanupDirs)
                {
                    try { if(Directory.Exists(dir)) { Directory.Delete(dir, true); } }
                    catch(IOException) { }
                    catch(UnauthorizedAccessException) { }
                }
            }
        }
    }
}
