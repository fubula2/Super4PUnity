using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

//Downloads one pinned ffmpeg build (only after the player agrees), checks its SHA-256, and unpacks just the ffmpeg program.
//The pinned file is the permanent "old-releases" copy of the static 6.0.1 x86-64 Linux build.
public static class FfmpegInstaller
{
    public const string Url = "https://johnvansickle.com/ffmpeg/old-releases/ffmpeg-6.0.1-amd64-static.tar.xz";
    public const string Sha256 = "28268bf402f1083833ea269331587f60a242848880073be8016501d864bd07a5";
    public const long ApproxBytes = 41164188;
    private const string InnerPath = "ffmpeg-6.0.1-amd64-static/ffmpeg";
    private const int BufferSize = 1 << 20;

    //Returns the path of the installed ffmpeg. Throws InvalidOperationException with a player-readable message on failure.
    public static async Task<string> InstallAsync(string toolsDir, Action<float> progress, CancellationToken ct)
    {
        var destDir = Path.Combine(toolsDir, "ffmpeg");
        var archive = Path.Combine(toolsDir, "ffmpeg-download.tar.xz.part");
        try
        {
            Directory.CreateDirectory(destDir);

            using(var response = await NetClient.Http.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? ApproxBytes;
                using(var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using(var output = new FileStream(archive, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
                {
                    var buffer = new byte[BufferSize];
                    long done = 0;
                    int read;
                    while((read = await input.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                        done += read;
                        progress?.Invoke(Math.Min(1f, (float)done / total) * 0.9f);
                    }
                }
            }

            using(var check = File.OpenRead(archive))
            {
                if(!Sha256Check.Matches(check, Sha256)) { throw new InvalidOperationException("The ffmpeg download did not match its checksum, so it was not installed."); }
            }

            var untar = await ProcessRunner.RunAsync("tar", new[] { "-xJf", archive, "-C", destDir, "--strip-components=1", InnerPath }, ct, 120000).ConfigureAwait(false);
            if(untar.ExitCode != 0) { throw new InvalidOperationException("Could not unpack ffmpeg (is 'tar' with xz support installed?): " + untar.Output.Trim()); }

            var path = FfmpegLocator.OwnPath(toolsDir);
            await ProcessRunner.RunAsync("chmod", new[] { "+x", path }, ct, 10000).ConfigureAwait(false);
            progress?.Invoke(0.95f);

            if(!await FfmpegLocator.WorksAsync(path, ct).ConfigureAwait(false)) { throw new InvalidOperationException("The downloaded ffmpeg does not run on this computer."); }
            progress?.Invoke(1f);
            return path;
        }
        catch(HttpRequestException e) { throw new InvalidOperationException("Could not download ffmpeg: " + e.Message); }
        catch(IOException e) { throw new InvalidOperationException("Could not save ffmpeg: " + e.Message); }
        catch(UnauthorizedAccessException e) { throw new InvalidOperationException("Could not save ffmpeg: " + e.Message); }
        finally
        {
            try { if(File.Exists(archive)) { File.Delete(archive); } } catch(IOException) { }
        }
    }
}
