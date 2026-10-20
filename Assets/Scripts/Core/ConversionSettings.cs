//How hard to push the CPU when converting clips: a few threads per ffmpeg (VP8 gains little beyond that on 720p),
//and as many ffmpegs at once as the cores allow. Works out from the machine, so a 2-core laptop and a 16-core desktop both stay busy.
public static class ConversionSettings
{
    public const int ThreadsPerJob = 2;

    public static int Workers(int cores, int clips)
    {
        var byCores = System.Math.Max(1, cores / ThreadsPerJob);
        return System.Math.Max(1, System.Math.Min(byCores, clips));
    }

    //libvpx realtime speed: higher is faster and softer; slower machines get the faster setting
    public static int CpuUsed(int cores) { return cores <= 2 ? 8 : 6; }
}
