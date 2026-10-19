using System;
using System.Globalization;

public static class TransferFormat
{
    public static string Speed(double bytesPerSecond)
    {
        if(double.IsNaN(bytesPerSecond) || bytesPerSecond <= 0) { return "0 KB/s"; }
        if(bytesPerSecond >= 1048576) { return (bytesPerSecond / 1048576).ToString("0.0", CultureInfo.InvariantCulture) + " MB/s"; }
        return Math.Round(bytesPerSecond / 1024).ToString("0", CultureInfo.InvariantCulture) + " KB/s";
    }

    public static string TimeLeft(double seconds)
    {
        if(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) { return "--:--"; }
        var total = (long)Math.Round(seconds);
        var h = total / 3600;
        var m = total % 3600 / 60;
        var s = total % 60;
        return h > 0 ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", h, m, s) : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", m, s);
    }

    public static string Size(long bytes)
    {
        if(bytes >= 1L << 30) { return (bytes / (double)(1L << 30)).ToString("0.0", CultureInfo.InvariantCulture) + " GB"; }
        return Math.Max(1, bytes / (1L << 20)).ToString(CultureInfo.InvariantCulture) + " MB";
    }
}

//Unlike Progress<T>, reports run immediately on the calling thread (no hop to the Unity main thread)
public sealed class InlineProgress<T> : IProgress<T>
{
    private readonly Action<T> handler;
    public InlineProgress(Action<T> handler) { this.handler = handler; }
    public void Report(T value) { handler?.Invoke(value); }
}
