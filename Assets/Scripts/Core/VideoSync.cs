using System;

public enum SyncKind { None, Nudge, Seek }

public readonly struct SyncCorrection
{
    public readonly SyncKind Kind;
    public readonly float PlaybackSpeed;
    public readonly double SeekTime;

    public SyncCorrection(SyncKind kind, float playbackSpeed, double seekTime)
    {
        Kind = kind;
        PlaybackSpeed = playbackSpeed;
        SeekTime = seekTime;
    }
}

//Keeps a looping video aligned to the music clock. Drift is positive when the video is ahead.
public static class VideoSync
{
    public const double Deadband = 0.02;
    public const double NudgeLimit = 0.10;
    public const float NudgeSpeed = 0.02f;

    //The music repeats every bar (4 s). Clips are usually a few frames longer than that, so the music phase must be
    //measured against the bar, not against the clip's own length (which would slip by the extra frames every loop).
    public const double DefaultBar = 4.0;

    public static double BarLength(double clipLength)
    {
        if(!(clipLength > 0) || double.IsInfinity(clipLength)) { return DefaultBar; }
        if(clipLength < DefaultBar / 2) { return clipLength; }
        return Math.Max(1.0, Math.Round(clipLength / DefaultBar, MidpointRounding.AwayFromZero)) * DefaultBar;
    }

    public static double Phase(double musicTime, double loopLength)
    {
        if (!(loopLength > 0)) { return 0; }
        var phase = musicTime % loopLength;
        return phase < 0 ? phase + loopLength : phase;
    }

    public static double Drift(double videoTime, double musicTime, double loopLength)
    {
        if (!(loopLength > 0)) { return 0; }
        var drift = videoTime - Phase(musicTime, loopLength);
        return drift - loopLength * Math.Round(drift / loopLength);
    }

    public static SyncCorrection Decide(double drift, double loopLength, double videoTime)
    {
        var magnitude = Math.Abs(drift);
        if (!(loopLength > 0) || double.IsNaN(drift) || magnitude <= Deadband)
        {
            return new SyncCorrection(SyncKind.None, 1f, 0);
        }

        if (magnitude <= NudgeLimit)
        {
            return new SyncCorrection(SyncKind.Nudge, drift > 0 ? 1f - NudgeSpeed : 1f + NudgeSpeed, 0);
        }

        return new SyncCorrection(SyncKind.Seek, 1f, Phase(videoTime - drift, loopLength));
    }
}
