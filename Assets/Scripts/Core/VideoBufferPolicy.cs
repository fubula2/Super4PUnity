//Size of the picture buffer each video player draws into. Smaller buffers cost phones less memory and fill-rate.
public static class VideoBufferPolicy
{
    public static (int Width, int Height) For(int systemMemoryMb, bool mobile)
    {
        if(!mobile) { return (1280, 720); }
        if(systemMemoryMb <= 2500) { return (854, 480); }
        if(systemMemoryMb <= 4500) { return (960, 540); }
        return (1280, 720);
    }
}
