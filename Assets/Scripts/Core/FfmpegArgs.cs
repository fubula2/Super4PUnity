using System.Collections.Generic;

//Command lines as argument lists (never one shell string), so paths with spaces or quotes stay intact
public static class FfmpegArgs
{
    //Converts a clip to VP8/Vorbis WebM, which Unity's Linux player can decode
    //The picture keeps its original size and frame rate; only the encoder effort is tuned for speed
    public static IReadOnlyList<string> Convert(string input, string output, int threads = 2, int cpuUsed = 6)
    {
        return new[]
        {
            "-y", "-nostdin", "-loglevel", "error",
            "-i", input,
            "-map", "0:v:0", "-map", "0:a:0?",
            "-c:v", "libvpx", "-pix_fmt", "yuv420p", "-b:v", "4M", "-crf", "10",
            "-deadline", "realtime", "-cpu-used", cpuUsed.ToString(), "-threads", threads.ToString(),
            "-c:a", "libvorbis", "-q:a", "4",
            "-f", "webm", output
        };
    }

    public static IReadOnlyList<string> ListEncoders() { return new[] { "-hide_banner", "-encoders" }; }

    //True when `ffmpeg -encoders` output lists both encoders used by Convert
    public static bool HasRequiredEncoders(string encodersOutput)
    {
        if(string.IsNullOrEmpty(encodersOutput)) { return false; }
        return HasEncoder(encodersOutput, "libvpx") && HasEncoder(encodersOutput, "libvorbis");
    }

    private static bool HasEncoder(string output, string name)
    {
        foreach(var line in output.Split('\n'))
        {
            var parts = line.Trim().Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length >= 2 && parts[1] == name) { return true; }
        }
        return false;
    }
}
