using System;
using System.IO;
using System.Linq;

public static class DiskInfo
{
    //Free bytes on the volume holding path, or null when it cannot be determined
    public static long? FreeBytes(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var drive = DriveInfo.GetDrives()
                .Where(d => d.IsReady && full.StartsWith(d.RootDirectory.FullName, StringComparison.Ordinal))
                .OrderByDescending(d => d.RootDirectory.FullName.Length)
                .FirstOrDefault();
            return drive?.AvailableFreeSpace;
        }
        catch(Exception) { return null; }
    }
}
