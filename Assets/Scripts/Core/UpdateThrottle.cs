using System;
using System.Globalization;

//When to check for updates again, and which version the player chose to skip
public static class UpdateThrottle
{
    public static string Stamp(DateTime utc) { return utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture); }

    public static bool ShouldCheck(string lastCheckStamp, DateTime nowUtc, TimeSpan interval)
    {
        if(string.IsNullOrWhiteSpace(lastCheckStamp)) { return true; }
        if(!DateTime.TryParse(lastCheckStamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)) { return true; }
        var last = parsed.ToUniversalTime();

        //A stamp in the future (clock changed) must not block checks indefinitely
        if(last > nowUtc) { return true; }
        return nowUtc - last >= interval;
    }

    public static bool IsSkipped(string version, string skippedVersion)
    {
        var a = AppVersion.Normalize(version);
        var b = AppVersion.Normalize(skippedVersion);
        return a != null && a == b;
    }
}
