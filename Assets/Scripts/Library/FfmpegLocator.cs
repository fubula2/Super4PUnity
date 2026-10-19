using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

//Finds a usable ffmpeg: the app's own downloaded copy first, then the system one. Usable means it lists libvpx and libvorbis.
public static class FfmpegLocator
{
    public static string OwnPath(string toolsDir) { return Path.Combine(toolsDir, "ffmpeg", "ffmpeg"); }

    public static async Task<string> FindAsync(string toolsDir, CancellationToken ct)
    {
        foreach(var candidate in Candidates(toolsDir))
        {
            if(await WorksAsync(candidate, ct).ConfigureAwait(false)) { return candidate; }
        }
        return null;
    }

    public static async Task<bool> WorksAsync(string path, CancellationToken ct)
    {
        try
        {
            var result = await ProcessRunner.RunAsync(path, FfmpegArgs.ListEncoders(), ct, 8000).ConfigureAwait(false);
            return result.ExitCode == 0 && FfmpegArgs.HasRequiredEncoders(result.Output);
        }
        catch(OperationCanceledException) { throw; }
        catch(Exception) { return false; }   //missing, not executable, wrong architecture, timed out
    }

    private static IEnumerable<string> Candidates(string toolsDir)
    {
        var own = OwnPath(toolsDir);
        if(File.Exists(own)) { yield return own; }
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach(var dir in path.Split(Path.PathSeparator))
        {
            if(string.IsNullOrWhiteSpace(dir)) { continue; }
            var candidate = Path.Combine(dir, "ffmpeg");
            if(File.Exists(candidate)) { yield return candidate; }
        }
    }
}
