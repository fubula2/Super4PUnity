using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public readonly struct PackProgress
{
    public readonly string Stage;
    public readonly float Fraction;
    public readonly long BytesDone;
    public readonly long BytesTotal;

    public PackProgress(string stage, float fraction, long bytesDone = 0, long bytesTotal = 0)
    {
        Stage = stage;
        Fraction = fraction;
        BytesDone = bytesDone;
        BytesTotal = bytesTotal;
    }
}

//Resolve -> download -> extract -> select -> install. Meant to run on a thread-pool thread (see DownloadManager);
//the callbacks are invoked on that thread, so UI code must not be touched from them.
public sealed class PackInstallJob
{
    private readonly PackDownloader downloader = new PackDownloader();

    //workDir is read from Application.persistentDataPath by the caller (main thread only)
    public async Task<InstallResult> RunAsync(string pageUrl, long expectedBytes, string modsDir, string workDir,
        Func<IReadOnlyList<string>, IReadOnlyList<string>> onSelect, Func<IReadOnlyList<string>, ConflictChoice> onConflict,
        Action<PackProgress> progress, CancellationToken ct)
    {
        string archive = null;
        try
        {
            progress?.Invoke(new PackProgress("Downloading", 0f));
            var direct = await downloader.ResolveDirectUrlAsync(pageUrl, ct).ConfigureAwait(false);
            archive = await downloader.DownloadAsync(direct, Path.Combine(workDir, "downloads"), expectedBytes,
                (done, total) => progress?.Invoke(new PackProgress("Downloading", total > 0 ? 0.8f * done / total : 0f, done, total)), ct).ConfigureAwait(false);

            progress?.Invoke(new PackProgress("Extracting", 0.8f));
            var extractProgress = new InlineProgress<float>(f => progress?.Invoke(new PackProgress("Extracting", 0.8f + f * 0.2f)));
            return await Task.Run(() => PackInstaller.Install(archive, modsDir, Path.Combine(workDir, "extract"), onSelect, onConflict, extractProgress, ct), ct).ConfigureAwait(false);
        }
        catch(OperationCanceledException) { return new InstallResult { Error = "Cancelled." }; }
        catch(InvalidOperationException e) { return new InstallResult { Error = e.Message }; }
        catch(HttpRequestException e) { return new InstallResult { Error = "Network error: " + e.Message }; }
        catch(IOException e) { return new InstallResult { Error = "File error: " + e.Message }; }
        finally
        {
            try { if(archive != null && File.Exists(archive)) { File.Delete(archive); } }
            catch(IOException) { }
        }
    }
}
