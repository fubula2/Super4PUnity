using System;

//Some clips cannot be decoded in their last frames (the player reports an error instead of "reached the end").
//These rules let the deck treat that error as the clip ending, and avoid it next time by pausing just before it.
public static class ClipEndPolicy
{
    //Pause this long before the end of a clip known to fail there
    public const double EarlyMargin = 0.2;
    //An error this close to the end (seconds) is the clip ending, not a real failure
    public const double NearEnd = 0.6;

    public static bool ShouldHoldEarly(bool knownBadEnd, double time, double length)
    {
        if(!knownBadEnd || !IsValid(length) || double.IsNaN(time)) { return false; }
        return time >= length - EarlyMargin;
    }

    public static bool IsErrorAtEnd(double lastGoodTime, double length)
    {
        if(!IsValid(length) || double.IsNaN(lastGoodTime) || lastGoodTime < 0) { return false; }
        return lastGoodTime >= length - NearEnd;
    }

    public static double HoldSeconds(double time, double length)
    {
        if(!IsValid(length) || double.IsNaN(time)) { return 0; }
        return Math.Max(0, length - time);
    }

    private static bool IsValid(double length) { return length > 0 && !double.IsNaN(length) && !double.IsInfinity(length); }
}
