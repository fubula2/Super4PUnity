using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

//Downloads one music file flat into <mods>/music. Streams to a .part file, checks it is an ogg, then moves it into place.
//Every await uses ConfigureAwait(false) so continuations never wait for Unity's main thread.
public static class MusicDownloadJob
{
    private const int BufferSize = 1 << 20;
    private const float HudShare = 0.8f;   //the progress strip treats 0.8 as a finished download (packs spend the rest extracting)
    private static readonly byte[] OggMagic = { (byte)'O', (byte)'g', (byte)'g', (byte)'S' };

    public static async Task<InstallResult> RunAsync(DownloadRequest request, Func<IReadOnlyList<string>, ConflictChoice> onConflict,
        Action<PackProgress> onProgress, CancellationToken ct)
    {
        string part = null;
        try
        {
            if(!FlatTarget.TryResolve(request.ModsDir, request.FileName, out var target)) { return Fail("This is not a usable music file name."); }
            if(!Uri.TryCreate(request.FileUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) { return Fail("The music link is not a secure web address."); }

            var skipped = new List<string>();
            if(File.Exists(target) && onConflict(new[] { Path.GetFileName(target) }) == ConflictChoice.Skip)
            {
                skipped.Add(Path.GetFileName(target));
                return new InstallResult { Ok = true, Skipped = skipped };
            }

            Directory.CreateDirectory(request.ModsDir);
            var workDir = Path.Combine(request.WorkDir, "downloads");
            Directory.CreateDirectory(workDir);
            part = Path.Combine(workDir, Guid.NewGuid().ToString("N") + ".part");

            onProgress(new PackProgress("Downloading", 0f));
            using(var response = await NetClient.Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? 0;
                using(var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using(var output = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
                {
                    var buffer = new byte[BufferSize];
                    long done = 0;
                    int read;
                    while((read = await input.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                        done += read;
                        onProgress(new PackProgress("Downloading", total > 0 ? HudShare * done / total : 0f, done, total));
                    }
                }
            }

            if(!LooksLikeOgg(part)) { return Fail("The download was not an ogg file (the host may have blocked it)."); }
            if(File.Exists(target)) { File.Delete(target); }
            File.Move(part, target);
            return new InstallResult { Ok = true, Installed = new[] { Path.GetFileName(target) }, Skipped = skipped };
        }
        catch(OperationCanceledException) { return Fail("Cancelled."); }
        catch(HttpRequestException e) { return Fail("Could not download: " + e.Message); }
        catch(IOException e) { return Fail("Could not save the file: " + e.Message); }
        catch(UnauthorizedAccessException e) { return Fail("Could not save the file: " + e.Message); }
        finally
        {
            if(part != null && File.Exists(part)) { try { File.Delete(part); } catch(IOException) { } }
        }
    }

    private static bool LooksLikeOgg(string path)
    {
        using(var f = File.OpenRead(path))
        {
            var head = new byte[4];
            return f.Read(head, 0, 4) == 4 && head[0] == OggMagic[0] && head[1] == OggMagic[1] && head[2] == OggMagic[2] && head[3] == OggMagic[3];
        }
    }

    private static InstallResult Fail(string message) { return new InstallResult { Ok = false, Error = message }; }
}
