public static class DiskSpace
{
    //Room for the archive plus its extracted contents
    public const double Factor = 2.2;

    public static bool HasRoom(long freeBytes, long packBytes)
    {
        if(packBytes <= 0 || freeBytes < 0) { return false; }
        return freeBytes >= packBytes * Factor;
    }
}
